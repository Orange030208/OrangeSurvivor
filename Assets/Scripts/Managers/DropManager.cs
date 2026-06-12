using System.Collections.Generic;
using Orange.Extraction;
using UnityEngine;

public class DropManager : MonoBehaviour
{
    private const int BASE_KILL_EXPERIENCE = 1;

    [SerializeField] private DropCollectionProfileSO dropCollectionProfile;
    [SerializeField] private List<DropSourceRuleData> dropRules = new();

    private IContentRandom random = new UnityContentRandom();

    private void OnEnable()
    {
        YokiFrame.EventKit.Type.Register<EntityDiedEvent>(OnEntityDied);
    }

    private void OnDisable()
    {
        YokiFrame.EventKit.Type.UnRegister<EntityDiedEvent>(OnEntityDied);
    }

    private void OnEntityDied(EntityDiedEvent deadEvent)
    {
        if (deadEvent.Reason == EntityDeathReason.WaveCleanup || deadEvent.Entity is not Enemy)
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
            Debug.LogError($"[DropManager] {dropSO.name} has no prefab assigned.", this);
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

        return RollDropProduct(rule, ResolveLuck(source));
    }

    private DropRollResult RollDropProduct(DropSourceRuleData rule, float luck)
    {
        if (!rule.HasProductRules)
        {
            return RollCollectionFromProfile(ResolveConfiguredDropProfile(), luck);
        }

        if (!TryDrawProduct(rule.Products, luck, out DropProductRuleData product))
        {
            return DropRollResult.None;
        }

        return new DropRollResult(product.Product, product.Quantity);
    }

    private DropRollResult RollCollectionFromProfile(DropCollectionProfileSO profile, float luck)
    {
        if (!TryDrawCollection(profile != null ? profile.Entries : null, luck, out CollectionSO collection))
        {
            return DropRollResult.None;
        }

        return new DropRollResult(collection, 1);
    }

    private DropCollectionProfileSO ResolveConfiguredDropProfile()
    {
        if (dropCollectionProfile != null)
        {
            return dropCollectionProfile;
        }

        return GameContentRuntime.TryGetProvider(out IGameContentProvider provider)
            ? provider.DropCollectionProfile
            : null;
    }

    private static bool TryDrawProduct(
        IReadOnlyList<DropProductRuleData> products,
        float luck,
        out DropProductRuleData selectedProduct)
    {
        selectedProduct = null;
        if (products == null || products.Count == 0)
        {
            return false;
        }

        WeightedExtractionPool<DropProductRuleData> extractionPool = new();
        for (int i = 0; i < products.Count; i++)
        {
            DropProductRuleData product = products[i];
            if (product == null || !product.IsValid)
            {
                continue;
            }

            extractionPool.AddEntry(
                ResolveProductEntryId(product, i),
                product,
                ResolveEffectiveWeight(product.BaseWeight, luck, product.LuckCoefficient));
        }

        if (!extractionPool.TryDrawOne(out ExtractionResult<DropProductRuleData> result))
        {
            return false;
        }

        selectedProduct = result.Item;
        return true;
    }

    private static bool TryDrawCollection(
        IReadOnlyList<DropCollectionProfileEntry> entries,
        float luck,
        out CollectionSO collection)
    {
        collection = null;
        if (entries == null || entries.Count == 0)
        {
            return false;
        }

        WeightedExtractionPool<CollectionSO> extractionPool = new();
        for (int i = 0; i < entries.Count; i++)
        {
            DropCollectionProfileEntry entry = entries[i];
            if (entry == null || !entry.IsValid)
            {
                continue;
            }

            extractionPool.AddEntry(
                entry.EntryId,
                entry.Collection,
                ResolveEffectiveWeight(entry.BaseWeight, luck, entry.LuckCoefficient));
        }

        if (!extractionPool.TryDrawOne(out ExtractionResult<CollectionSO> result))
        {
            return false;
        }

        collection = result.Item;
        return collection != null;
    }

    private static float ResolveEffectiveWeight(float baseWeight, float luck, float luckCoefficient)
    {
        float multiplier = 1f + luck * luckCoefficient / DropSourceRuleData.LUCK_WEIGHT_DIVISOR;
        return Mathf.Max(0f, baseWeight * Mathf.Max(0f, multiplier));
    }

    private static string ResolveProductEntryId(DropProductRuleData product, int index)
    {
        string productName = product?.Product != null ? product.Product.name : "None";
        return $"DropProduct_{index}_{productName}";
    }

    private static float ResolveLuck(Entity source)
    {
        PropertiesManager propertiesManager = ResolvePropertiesManager(source);
        return propertiesManager != null ? propertiesManager.GetPropValue(PropType.Luck) : 0f;
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
