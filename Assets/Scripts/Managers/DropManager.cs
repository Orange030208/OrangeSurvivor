using UnityEngine;
using System.Collections.Generic;

public class DropManager : MonoBehaviour
{
    private const int BASE_KILL_EXPERIENCE = 1;

    [SerializeField] private ContentPoolSO dropPool;
    [SerializeField] private List<DropSourceRuleData> dropRules = new();

    private readonly DropContentRoller contentRoller = new();
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
        DropRollResult dropResult = RollDropForSource(dropSource, deadEvent.Source, progressionSnapshot.WaveNumber);
        CollectionSO dropSO = dropResult.Collection;

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
            coin.ConfigureReward(new CoinRewardData(dropResult.Quantity));
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

    public DropRollResult RollDropForSource(DropSourceInfo dropSource, Entity source, int waveNumber)
    {
        DropSourceRuleData rule = ResolveSourceRule(dropSource, dropRules);
        if (rule == null)
        {
            return DropRollResult.None;
        }

        float chance = rule.EvaluateDropChance(ResolveLuck(source));
        if (chance <= 0f || random.Value01() > chance)
        {
            return DropRollResult.None;
        }

        return RollDropProduct(rule, source, waveNumber);
    }

    private DropRollResult RollDropProduct(DropSourceRuleData rule, Entity source, int waveNumber)
    {
        ContentPoolSO configuredPool = ResolveConfiguredDropPool();
        ContentRollContext context = CreateDropRollContext(configuredPool, source, waveNumber);
        ContentRollScope scope = CreateRollScope(configuredPool);
        if (!rule.HasProductRules)
        {
            return RollCollectionFromPool(configuredPool, context, scope);
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
            return DropRollResult.None;
        }

        ContentRollItem productItem = contentRoller.RollProduct(
            productEntryBuffer,
            context,
            scope,
            RunContentHistoryRuntime.Current);
        if (productItem.Content == null)
        {
            return DropRollResult.None;
        }

        if (productItem.Content is CollectionSO collection)
        {
            int quantity = ResolveDropQuantity(productItem);
            return new DropRollResult(collection, quantity);
        }

        return productItem.Content is ContentPoolSO nestedPool
            ? RollCollectionFromPool(nestedPool, context, scope)
            : DropRollResult.None;
    }

    private DropRollResult RollCollectionFromPool(ContentPoolSO pool, ContentRollContext context, ContentRollScope scope)
    {
        if (pool == null)
        {
            return DropRollResult.None;
        }

        ContentRollItem rollItem = contentRoller.RollCollection(
            pool,
            context,
            scope,
            RunContentHistoryRuntime.Current);
        return rollItem.Content != null
            ? new DropRollResult(rollItem.Content as CollectionSO, ResolveDropQuantity(rollItem))
            : DropRollResult.None;
    }

    private static int ResolveDropQuantity(ContentRollItem rollItem)
    {
        return rollItem.TryGetMetadata(out DropQuantityMetadata quantityMetadata)
            ? quantityMetadata.Quantity
            : 1;
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
                snapshot.ShopRerollBasePrice,
                snapshot.ShopRerollStepPrice,
                snapshot.DangerTier);
        }

        return new ContentRollContext(
            ContentPoolScopeIds.Drop,
            player,
            progressionSnapshot: snapshot,
            historyScope: CreateRollScope(pool).ToHistoryScope(),
            history: RunContentHistoryRuntime.Current.State,
            source: source,
            propertiesManager: ResolvePropertiesManager(source));
    }

    private static ContentRollScope CreateRollScope(ContentPoolSO pool)
    {
        return DropContentRoller.CreateScope(pool);
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
