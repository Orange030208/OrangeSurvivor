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
    [Header("Gameplay Lists")]
    [SerializeField] private WeaponDataListSO weaponDataList;
    [SerializeField] private AccessoryDataListSO accessoryDataList;
    [SerializeField] private CharacterDataSO[] characters = System.Array.Empty<CharacterDataSO>();

    [Header("Gameplay Config")]
    [SerializeField] private PlayerLevelConfigSO playerLevelConfig;
    [SerializeField] private ContentPoolSO upgradeCardPool;
    [SerializeField] private StageDefinitionSO defaultStageDefinition;

    [Header("Content Pools")]
    [SerializeField] private ContentPoolSO chestRewardPool;
    [SerializeField] private ContentPoolSO shopPool;
    [SerializeField] private ContentPoolSO dropPool;
    [SerializeField] private ContentPoolSO waveSpawnPool;
    [SerializeField] private ContentPoolSO weaponRewardPool;

    [Header("Prefabs")]
    [SerializeField] private Player defaultPlayerPrefab;
    [SerializeField] private Weapon defaultWeaponPrefab;
    [SerializeField] private DamageTextFlow damageTextPrefab;

    [Header("Presentation")]
    [SerializeField] private PropPresentationCatalogSO propPresentationCatalog;
    [SerializeField] private CardQualityPresentationCatalogSO cardQualityPresentationCatalog;
    [SerializeField] private ItemQualityVisualConfigSO itemQualityVisualConfig;
    [SerializeField] private DamageTextVisualConfigSO damageTextVisualConfig;
    [SerializeField] private Material itemQualityIconEffectMaterial;

    public IReadOnlyList<WeaponDataSO> Weapons => weaponDataList != null && weaponDataList.Weapons != null
        ? weaponDataList.Weapons
        : System.Array.Empty<WeaponDataSO>();

    public IReadOnlyList<AccessoryDataSO> Accessories => accessoryDataList != null && accessoryDataList.Accessories != null
        ? accessoryDataList.Accessories
        : System.Array.Empty<AccessoryDataSO>();

    public IReadOnlyList<CharacterDataSO> Characters => characters ?? System.Array.Empty<CharacterDataSO>();
    public Player DefaultPlayerPrefab => defaultPlayerPrefab;
    public Weapon DefaultWeaponPrefab => defaultWeaponPrefab;
    public PlayerLevelConfigSO PlayerLevelConfig => playerLevelConfig;
    public ContentPoolSO UpgradeCardPool => upgradeCardPool;
    public ContentPoolSO ChestRewardPool => chestRewardPool;
    public ContentPoolSO ShopPool => shopPool;
    public ContentPoolSO DropPool => dropPool;
    public ContentPoolSO WaveSpawnPool => waveSpawnPool;
    public ContentPoolSO WeaponRewardPool => weaponRewardPool;
    public CardQualityPresentationCatalogSO CardQualityPresentationCatalog => cardQualityPresentationCatalog;
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

        ValidateRequired(weaponDataList, nameof(weaponDataList), errors);
        ValidateRequired(accessoryDataList, nameof(accessoryDataList), errors);
        ValidateRequired(playerLevelConfig, nameof(playerLevelConfig), errors);
        ValidateRequired(upgradeCardPool, nameof(upgradeCardPool), errors);
        ValidatePoolPurpose(upgradeCardPool, ContentPoolPurpose.UpgradeCard, nameof(upgradeCardPool), errors);
        ValidateRequired(chestRewardPool, nameof(chestRewardPool), errors);
        ValidatePoolPurpose(chestRewardPool, ContentPoolPurpose.ChestReward, nameof(chestRewardPool), errors);
        ValidateRequired(shopPool, nameof(shopPool), errors);
        ValidatePoolPurpose(shopPool, ContentPoolPurpose.Shop, nameof(shopPool), errors);
        ValidateRequired(dropPool, nameof(dropPool), errors);
        ValidatePoolPurpose(dropPool, ContentPoolPurpose.Drop, nameof(dropPool), errors);
        ValidateRequired(weaponRewardPool, nameof(weaponRewardPool), errors);
        ValidatePoolPurpose(weaponRewardPool, ContentPoolPurpose.WeaponReward, nameof(weaponRewardPool), errors);
        ValidateRequired(defaultStageDefinition, nameof(defaultStageDefinition), errors);
        ValidateRequired(defaultPlayerPrefab, nameof(defaultPlayerPrefab), errors);
        ValidateRequired(defaultWeaponPrefab, nameof(defaultWeaponPrefab), errors);
        ValidateRequired(damageTextPrefab, nameof(damageTextPrefab), errors);
        ValidateRequired(propPresentationCatalog, nameof(propPresentationCatalog), errors);
        ValidateRequired(cardQualityPresentationCatalog, nameof(cardQualityPresentationCatalog), errors);
        ValidateRequired(itemQualityVisualConfig, nameof(itemQualityVisualConfig), errors);
        ValidateRequired(damageTextVisualConfig, nameof(damageTextVisualConfig), errors);
        ValidateRequired(itemQualityIconEffectMaterial, nameof(itemQualityIconEffectMaterial), errors);

        if (Weapons.Count == 0)
        {
            errors.Add($"{nameof(GameContentCatalogSO)} '{name}' has no weapons.");
        }

        if (Accessories.Count == 0)
        {
            errors.Add($"{nameof(GameContentCatalogSO)} '{name}' has no accessories.");
        }

        if (Characters.Count == 0)
        {
            errors.Add($"{nameof(GameContentCatalogSO)} '{name}' has no characters.");
        }

        ValidateNoDuplicates(Weapons, "weapon", errors);
        ValidateNoDuplicates(Accessories, "accessory", errors);
        ValidateNoDuplicates(Characters, "character", errors);
        return errors.Count == initialCount;
    }

    private void OnValidate()
    {
        if (characters == null)
        {
            characters = System.Array.Empty<CharacterDataSO>();
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

    private static void ValidatePoolPurpose(
        ContentPoolSO pool,
        ContentPoolPurpose expectedPurpose,
        string fieldName,
        List<string> errors)
    {
        if (pool == null || pool.Purpose == expectedPurpose)
        {
            return;
        }

        errors.Add(
            $"{nameof(GameContentCatalogSO)} field '{fieldName}' expects {expectedPurpose} but references {pool.Purpose}.");
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
