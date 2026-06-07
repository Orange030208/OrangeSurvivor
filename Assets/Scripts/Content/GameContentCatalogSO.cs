using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 运行时内容目录资产，用显式序列化引用替代固定 Resources 路径。
/// 运行时代码应通过 Provider 读取这里的内容，不再按字符串路径加载资源。
/// </summary>
[CreateAssetMenu(
    fileName = "Game Content Catalog",
    menuName = ScriptableObjectMenuPaths.SYSTEMS_ROOT + "Content/Game Content Catalog",
    order = 0)]
public sealed class GameContentCatalogSO : ScriptableObject
{
    // 这些字段保持显式序列化，方便在进 Play Mode 前从 Inspector 直接发现缺失引用。
    // 运行时代码统一读取下方只读属性，不直接修改 Catalog。
    [Header("玩法列表")]
    [SerializeField] private WeaponDataSO[] weapons = System.Array.Empty<WeaponDataSO>();
    [SerializeField] private AccessoryDataListSO accessoryDataList;
    [SerializeField] private CharacterDataSO defaultCharacter;
    [SerializeField] private RewardCardSO[] starterCards = System.Array.Empty<RewardCardSO>();

    [Header("玩法配置")]
    [SerializeField] private PlayerLevelConfigSO playerLevelConfig;
    [SerializeField] private RunProgressionProfileSO runProgressionProfile;
    [SerializeField] private ContentPoolSO upgradeCardPool;
    [SerializeField] private StageDefinitionSO defaultStageDefinition;

    [Header("内容池")]
    [SerializeField] private ContentPoolSO chestRewardPool;
    [SerializeField] private ContentPoolSO shopPool;
    [SerializeField] private ContentPoolSO dropPool;
    [SerializeField] private ContentPoolSO waveSpawnPool;
    [SerializeField] private ContentPoolSO weaponRewardPool;

    [Header("预制体")]
    [SerializeField] private Player defaultPlayerPrefab;
    [SerializeField] private Weapon defaultWeaponPrefab;
    [SerializeField] private DamageTextFlow damageTextPrefab;

    [Header("表现配置")]
    [SerializeField] private PropPresentationCatalogSO propPresentationCatalog;
    [SerializeField] private ItemQualityVisualConfigSO itemQualityVisualConfig;
    [SerializeField] private DamageTextVisualConfigSO damageTextVisualConfig;
    [SerializeField] private Material itemQualityIconEffectMaterial;

    public IReadOnlyList<WeaponDataSO> Weapons => weapons ?? System.Array.Empty<WeaponDataSO>();

    public IReadOnlyList<AccessoryDataSO> Accessories => accessoryDataList != null && accessoryDataList.Accessories != null
        ? accessoryDataList.Accessories
        : System.Array.Empty<AccessoryDataSO>();

    public CharacterDataSO DefaultCharacter => defaultCharacter;
    public IReadOnlyList<RewardCardSO> StarterCards => starterCards ?? System.Array.Empty<RewardCardSO>();
    public Player DefaultPlayerPrefab => defaultPlayerPrefab;
    public Weapon DefaultWeaponPrefab => defaultWeaponPrefab;
    public PlayerLevelConfigSO PlayerLevelConfig => playerLevelConfig;
    public RunProgressionProfileSO RunProgressionProfile => runProgressionProfile;
    public ContentPoolSO UpgradeCardPool => upgradeCardPool;
    public ContentPoolSO ChestRewardPool => chestRewardPool;
    public ContentPoolSO ShopPool => shopPool;
    public ContentPoolSO DropPool => dropPool;
    public ContentPoolSO WaveSpawnPool => waveSpawnPool;
    public ContentPoolSO WeaponRewardPool => weaponRewardPool;
    public PropPresentationCatalogSO PropPresentationCatalog => propPresentationCatalog;
    public StageDefinitionSO DefaultStageDefinition => defaultStageDefinition;
    public DamageTextFlow DamageTextPrefab => damageTextPrefab;
    public DamageTextVisualConfigSO DamageTextVisualConfig => damageTextVisualConfig;
    public ItemQualityVisualConfigSO ItemQualityVisualConfig => itemQualityVisualConfig;
    public Material ItemQualityIconEffectMaterial => itemQualityIconEffectMaterial;

    /// <summary>
    /// 校验运行时入口所需的关键引用。
    /// 这里会收集全部问题，方便一次性修完 Catalog 配置，而不是遇到第一个缺失就中断。
    /// </summary>
    public bool ValidateCatalog(List<string> errors)
    {
        errors ??= new List<string>();
        int initialCount = errors.Count;

        if (Weapons.Count == 0)
        {
            errors.Add($"{nameof(GameContentCatalogSO)} '{name}' has no weapons.");
        }

        ValidateRequired(accessoryDataList, nameof(accessoryDataList), errors);
        ValidateRequired(playerLevelConfig, nameof(playerLevelConfig), errors);
        ValidateRequired(runProgressionProfile, nameof(runProgressionProfile), errors);
        ValidateRequired(upgradeCardPool, nameof(upgradeCardPool), errors);
        ValidatePoolContents<RewardCardSO>(upgradeCardPool, nameof(upgradeCardPool), errors);
        ValidateRequired(chestRewardPool, nameof(chestRewardPool), errors);
        ValidatePoolContents<AccessoryDataSO>(chestRewardPool, nameof(chestRewardPool), errors);
        ValidateRequired(shopPool, nameof(shopPool), errors);
        ValidatePoolContents<ItemDataSO>(shopPool, nameof(shopPool), errors);
        ValidateRequired(dropPool, nameof(dropPool), errors);
        ValidatePoolContents<CollectionSO>(dropPool, nameof(dropPool), errors);
        ValidateRequired(waveSpawnPool, nameof(waveSpawnPool), errors);
        ValidatePoolContents(waveSpawnPool, nameof(waveSpawnPool), errors, content => content is EnemySO or WaveSpawnPackSO);
        ValidateRequired(weaponRewardPool, nameof(weaponRewardPool), errors);
        ValidatePoolContents<WeaponDataSO>(weaponRewardPool, nameof(weaponRewardPool), errors);
        ValidateRequired(defaultStageDefinition, nameof(defaultStageDefinition), errors);
        ValidateRequired(defaultPlayerPrefab, nameof(defaultPlayerPrefab), errors);
        ValidateRequired(defaultWeaponPrefab, nameof(defaultWeaponPrefab), errors);
        ValidateRequired(damageTextPrefab, nameof(damageTextPrefab), errors);
        ValidateRequired(propPresentationCatalog, nameof(propPresentationCatalog), errors);
        ValidateRequired(itemQualityVisualConfig, nameof(itemQualityVisualConfig), errors);
        ValidateRequired(damageTextVisualConfig, nameof(damageTextVisualConfig), errors);
        ValidateRequired(itemQualityIconEffectMaterial, nameof(itemQualityIconEffectMaterial), errors);

        if (Accessories.Count == 0)
        {
            errors.Add($"{nameof(GameContentCatalogSO)} '{name}' has no accessories.");
        }

        if (DefaultCharacter == null)
        {
            errors.Add($"{nameof(GameContentCatalogSO)} '{name}' is missing default character.");
        }

        ValidateNoDuplicates(Weapons, "weapon", errors);
        ValidateNoDuplicates(Accessories, "accessory", errors);
        ValidateNoDuplicates(StarterCards, "starter card", errors);
        return errors.Count == initialCount;
    }

    private void OnValidate()
    {
        if (starterCards == null)
        {
            starterCards = System.Array.Empty<RewardCardSO>();
        }

        // OnValidate 只打 warning，避免编辑资产时打断流程；真正进入运行时由 Bootstrap 打 error。
        List<string> errors = new();
        ValidateCatalog(errors);
        for (int i = 0; i < errors.Count; i++)
        {
            Debug.LogWarning(errors[i], this);
        }
    }

    private static void ValidateRequired(UnityEngine.Object value, string fieldName, List<string> errors)
    {
        if (value == null)
        {
            errors.Add($"{nameof(GameContentCatalogSO)} is missing required field '{fieldName}'.");
        }
    }

    private static void ValidatePoolContents<T>(
        ContentPoolSO pool,
        string fieldName,
        List<string> errors)
        where T : UnityEngine.Object
    {
        ValidatePoolContents(pool, fieldName, errors, content => content is T);
    }

    private static void ValidatePoolContents(
        ContentPoolSO pool,
        string fieldName,
        List<string> errors,
        System.Predicate<UnityEngine.Object> isAllowedContent)
    {
        if (pool == null || pool.Entries == null || isAllowedContent == null)
        {
            return;
        }

        for (int i = 0; i < pool.Entries.Count; i++)
        {
            ContentPoolEntry entry = pool.Entries[i];
            if (entry?.Content == null || isAllowedContent(entry.Content))
            {
                continue;
            }

            errors.Add(
                $"{nameof(GameContentCatalogSO)} field '{fieldName}' contains unsupported entry '{entry.EntryId}' ({entry.Content.GetType().Name}).");
        }
    }

    private static void ValidateNoDuplicates<T>(IReadOnlyList<T> items, string label, List<string> errors)
        where T : UnityEngine.Object
    {
        if (items == null || items.Count == 0)
        {
            return;
        }

        // 重复引用通常代表同一份内容会在运行时列表里出现两次，应在 Catalog 层提前暴露。
        HashSet<T> seen = new();
        for (int i = 0; i < items.Count; i++)
        {
            T item = items[i];
            if (item == null)
            {
                errors.Add($"{nameof(GameContentCatalogSO)} has a null {label} entry at index {i}.");
                continue;
            }

            if (!seen.Add(item))
            {
                errors.Add($"{nameof(GameContentCatalogSO)} has duplicate {label} '{item.name}'.");
            }
        }
    }
}
