using UnityEngine;
using System.Collections.Generic;

public class DropManager : MonoBehaviour
{
    private const int BASE_KILL_EXPERIENCE = 1;

    private static readonly CoinRewardData FixedCoinReward = new(1);

    [SerializeField] private ContentPoolSO dropPool;
    [SerializeField] private List<DropSourceRuleData> dropRules = new();

    private ContentPoolRollService contentPoolRollService = new();
    private readonly ContentHistoryState contentHistoryState = new();
    private readonly List<ContentPoolEntry> productEntryBuffer = new();
    private IContentRandom random = new UnityContentRandom();

    private void OnEnable()
    {
        GameEventBus.Subscribe<EntityDiedEvent>(OnEntityDied);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<EntityDiedEvent>(OnEntityDied);
    }

    private void OnEntityDied(EntityDiedEvent deadEvent)
    {
        if (deadEvent.Reason == EntityDeathReason.WaveCleanup)
        {
            return;
        }

        if (deadEvent.Entity is not Enemy)
        {
            return;
        }

        Enemy defeatedEnemy = deadEvent.Entity as Enemy;
        DropSourceInfo dropSource = DropSourceInfo.FromEnemy(defeatedEnemy);
        TryGrantKillExperience(deadEvent.Source, ResolveKillExperience(dropSource, dropRules));

        RunProgressionSnapshot progressionSnapshot = RunProgressionRuntime.CurrentSnapshot;
        CollectionSO dropSO = RollDropForSource(dropSource, deadEvent.Source, progressionSnapshot.WaveNumber);

        if (dropSO == null)
        {
            return;
        }

        if (dropSO.prefab == null)
        {
            Debug.LogError($"[DropManager] {dropSO?.name} has no prefab assigned.", this);
            return;
        }

        Collection instance = Instantiate(dropSO.prefab, deadEvent.Position, Quaternion.identity, transform);
        instance.Configure(dropSO);
        if (dropSO.prefab is Coin && instance is Coin coin)
        {
            coin.ConfigureReward(FixedCoinReward);
        }
    }

    public static bool TryGrantKillExperience(Entity source)
    {
        return TryGrantKillExperience(source, BASE_KILL_EXPERIENCE);
    }

    public static int ResolveKillExperience(
        DropSourceInfo dropSource,
        IReadOnlyList<DropSourceRuleData> rules)
    {
        DropSourceRuleData rule = ResolveSourceRule(dropSource, rules);
        return rule != null ? rule.KillExperience : BASE_KILL_EXPERIENCE;
    }

    public static DropSourceRuleData ResolveSourceRule(
        DropSourceInfo dropSource,
        IReadOnlyList<DropSourceRuleData> rules)
    {
        if (rules == null || rules.Count == 0)
        {
            return null;
        }

        DropSourceRuleData bestRule = null;
        int bestScore = -1;
        for (int i = 0; i < rules.Count; i++)
        {
            DropSourceRuleData rule = rules[i];
            if (rule == null)
            {
                continue;
            }

            int score = rule.GetMatchScore(dropSource);
            if (score > bestScore)
            {
                bestRule = rule;
                bestScore = score;
            }
        }

        return bestRule;
    }

    public static bool TryGrantKillExperience(Entity source, int baseExperience)
    {
        PlayerLevel playerLevel = ResolvePlayerLevel(source);
        if (playerLevel == null || baseExperience <= 0)
        {
            return false;
        }

        playerLevel.AddXP(baseExperience);
        return true;
    }

    private static PlayerLevel ResolvePlayerLevel(Entity source)
    {
        if (source == null)
        {
            return null;
        }

        if (source.TryGetComponent(out PlayerLevel playerLevel))
        {
            return playerLevel;
        }

        if (source is Weapon weapon && weapon.Owner != null &&
            weapon.Owner.TryGetComponent(out PlayerLevel ownerPlayerLevel))
        {
            return ownerPlayerLevel;
        }

        return null;
    }

    public CollectionSO RollDropForSource(DropSourceInfo dropSource, Entity source, int waveNumber)
    {
        DropSourceRuleData rule = ResolveSourceRule(dropSource, dropRules);
        if (rule == null)
        {
            return null;
        }

        float chance = rule.EvaluateDropChance(ResolveLuck(source));
        if (chance <= 0f || random.Value01() > chance)
        {
            return null;
        }

        return RollDropProduct(rule, source, waveNumber);
    }

    private CollectionSO RollDropProduct(DropSourceRuleData rule, Entity source, int waveNumber)
    {
        ContentPoolSO configuredPool = ResolveConfiguredDropPool();
        ContentRollContext context = CreateDropRollContext(configuredPool, source, waveNumber);
        if (!rule.HasProductRules)
        {
            return RollCollectionFromPool(configuredPool, context);
        }

        productEntryBuffer.Clear();
        IReadOnlyList<DropProductRuleData> products = rule.Products;
        for (int i = 0; i < products.Count; i++)
        {
            ContentPoolEntry entry = products[i]?.CreateEntry(configuredPool, i);
            if (entry != null)
            {
                productEntryBuffer.Add(entry);
            }
        }

        if (productEntryBuffer.Count == 0)
        {
            return null;
        }

        ContentRollResult productResult = contentPoolRollService.Roll(
            ContentPoolScopeIds.Drop,
            productEntryBuffer,
            context,
            1,
            false,
            entry => entry.Content is CollectionSO or ContentPoolSO);
        if (!productResult.HasAny)
        {
            return null;
        }

        if (productResult.Items[0].Content is CollectionSO collection)
        {
            return collection;
        }

        return productResult.Items[0].Content is ContentPoolSO nestedPool
            ? RollCollectionFromPool(nestedPool, context)
            : null;
    }

    private CollectionSO RollCollectionFromPool(ContentPoolSO pool, ContentRollContext context)
    {
        if (pool == null)
        {
            return null;
        }

        ContentRollResult configuredResult = contentPoolRollService.Roll(
            pool,
            context,
            1,
            entry => entry.Content is CollectionSO);
        return configuredResult.HasAny ? configuredResult.Items[0].Content as CollectionSO : null;
    }

    private ContentPoolSO ResolveConfiguredDropPool()
    {
        if (dropPool != null)
        {
            return dropPool;
        }

        if (GameContentRuntime.TryGetProvider(out IGameContentProvider provider) && provider.DropPool != null)
        {
            return provider.DropPool;
        }

        return null;
    }

    private ContentRollContext CreateDropRollContext(ContentPoolSO pool, Entity source, int waveNumber)
    {
        Player player = ResolvePlayer(source);
        RunProgressionSnapshot snapshot = RunProgressionRuntime.CurrentSnapshot;
        if (snapshot.WaveNumber != Mathf.Max(1, waveNumber))
        {
            snapshot = new RunProgressionSnapshot(
                waveNumber,
                snapshot.TotalWaves,
                snapshot.RunMinutes,
                snapshot.EndlessLoop,
                snapshot.DifficultyCoefficient,
                snapshot.EconomyCoefficient,
                snapshot.ShopPriceMultiplier,
                snapshot.DangerTier);
        }

        return new ContentRollContext(
            ContentPoolScopeIds.Drop,
            player,
            progressionSnapshot: snapshot,
            historyScope: CreateHistoryScope(pool),
            history: contentHistoryState,
            source: source,
            propertiesManager: ResolvePropertiesManager(source));
    }

    private static ContentHistoryScope CreateHistoryScope(ContentPoolSO pool)
    {
        string poolId = pool != null ? pool.name : ContentPoolScopeIds.Drop;
        return new ContentHistoryScope(ContentPoolScopeIds.Drop, poolId);
    }

    private static float ResolveLuck(Entity source)
    {
        PropertiesManager propertiesManager = ResolvePropertiesManager(source);
        return propertiesManager != null ? propertiesManager.GetPropValue(PropType.Luck) : 0f;
    }

    private static Player ResolvePlayer(Entity source)
    {
        if (source is Player player)
        {
            return player;
        }

        return source is Weapon weapon && weapon.Owner is Player ownerPlayer
            ? ownerPlayer
            : null;
    }

    private static PropertiesManager ResolvePropertiesManager(Entity source)
    {
        if (source == null)
        {
            return null;
        }

        if (source.TryGetComponent(out PropertiesManager propertiesManager))
        {
            return propertiesManager;
        }

        return source is Weapon weapon && weapon.Owner != null &&
               weapon.Owner.TryGetComponent(out PropertiesManager ownerPropertiesManager)
            ? ownerPropertiesManager
            : null;
    }

}
