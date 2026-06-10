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
    [Header("玩法列表")]
    [SerializeField] private WeaponDataSO[] weapons = System.Array.Empty<WeaponDataSO>();
    [SerializeField] private AccessoryDataListSO accessoryDataList;
    [SerializeField] private CharacterDataSO defaultCharacter;
    [SerializeField] private RewardCardSO[] rewardCards = System.Array.Empty<RewardCardSO>();
    [SerializeField] private RewardCardSO[] starterCards = System.Array.Empty<RewardCardSO>();
    [SerializeField] private CollectionSO[] collections = System.Array.Empty<CollectionSO>();
    [SerializeField] private EnemySO[] enemies = System.Array.Empty<EnemySO>();
    [SerializeField] private BuffDataSO[] buffs = System.Array.Empty<BuffDataSO>();

    [Header("玩法配置")]
    [SerializeField] private PlayerLevelConfigSO playerLevelConfig;
    [SerializeField] private RunProgressionProfileSO runProgressionProfile;
    [SerializeField] private StageDirectorProfileSO defaultStageDirectorProfile;
    [SerializeField] private ContentTierWeightProfileSO contentTierWeightProfile;
    [SerializeField] private DropCollectionProfileSO dropCollectionProfile;

    [Header("预制体")]
    [SerializeField] private Player defaultPlayerPrefab;
    [SerializeField] private Weapon defaultWeaponPrefab;
    [SerializeField] private DamageTextFlow damageTextPrefab;

    [Header("表现配置")]
    [SerializeField] private PropPresentationCatalogSO propPresentationCatalog;
    [SerializeField] private DamageTextVisualConfigSO damageTextVisualConfig;
    [SerializeField] private Material itemQualityIconEffectMaterial;
    [SerializeField] private TierColorPaletteSO tierColorPalette;

    public IReadOnlyList<WeaponDataSO> Weapons => weapons ?? System.Array.Empty<WeaponDataSO>();
    public IReadOnlyList<AccessoryDataSO> Accessories => accessoryDataList != null && accessoryDataList.Accessories != null
        ? accessoryDataList.Accessories
        : System.Array.Empty<AccessoryDataSO>();
    public IReadOnlyList<RewardCardSO> RewardCards => rewardCards ?? System.Array.Empty<RewardCardSO>();
    public IReadOnlyList<RewardCardSO> StarterCards => starterCards ?? System.Array.Empty<RewardCardSO>();
    public IReadOnlyList<CollectionSO> Collections => collections ?? System.Array.Empty<CollectionSO>();
    public IReadOnlyList<EnemySO> Enemies => enemies ?? System.Array.Empty<EnemySO>();
    public IReadOnlyList<BuffDataSO> Buffs => buffs ?? System.Array.Empty<BuffDataSO>();
    public CharacterDataSO DefaultCharacter => defaultCharacter;
    public Player DefaultPlayerPrefab => defaultPlayerPrefab;
    public Weapon DefaultWeaponPrefab => defaultWeaponPrefab;
    public PlayerLevelConfigSO PlayerLevelConfig => playerLevelConfig;
    public RunProgressionProfileSO RunProgressionProfile => runProgressionProfile;
    public StageDirectorProfileSO DefaultStageDirectorProfile => defaultStageDirectorProfile;
    public ContentTierWeightProfileSO ContentTierWeightProfile => contentTierWeightProfile;
    public DropCollectionProfileSO DropCollectionProfile => dropCollectionProfile;
    public PropPresentationCatalogSO PropPresentationCatalog => propPresentationCatalog;
    public DamageTextFlow DamageTextPrefab => damageTextPrefab;
    public DamageTextVisualConfigSO DamageTextVisualConfig => damageTextVisualConfig;
    public Material ItemQualityIconEffectMaterial => itemQualityIconEffectMaterial;
    public TierColorPaletteSO TierColorPalette => tierColorPalette;

    public bool ValidateCatalog(List<string> errors)
    {
        errors ??= new List<string>();
        int initialCount = errors.Count;

        ValidateRequired(playerLevelConfig, nameof(playerLevelConfig), errors);
        ValidateRequired(runProgressionProfile, nameof(runProgressionProfile), errors);
        ValidateRequired(defaultStageDirectorProfile, nameof(defaultStageDirectorProfile), errors);
        ValidateRequired(contentTierWeightProfile, nameof(contentTierWeightProfile), errors);
        ValidateRequired(dropCollectionProfile, nameof(dropCollectionProfile), errors);
        ValidateRequired(defaultPlayerPrefab, nameof(defaultPlayerPrefab), errors);
        ValidateRequired(defaultWeaponPrefab, nameof(defaultWeaponPrefab), errors);
        ValidateRequired(damageTextPrefab, nameof(damageTextPrefab), errors);
        ValidateRequired(propPresentationCatalog, nameof(propPresentationCatalog), errors);
        ValidateRequired(damageTextVisualConfig, nameof(damageTextVisualConfig), errors);
        ValidateRequired(itemQualityIconEffectMaterial, nameof(itemQualityIconEffectMaterial), errors);
        ValidateRequired(tierColorPalette, nameof(tierColorPalette), errors);

        if (Weapons.Count == 0)
        {
            errors.Add($"{nameof(GameContentCatalogSO)} '{name}' has no weapons.");
        }

        if (Accessories.Count == 0)
        {
            errors.Add($"{nameof(GameContentCatalogSO)} '{name}' has no accessories.");
        }

        if (RewardCards.Count == 0)
        {
            errors.Add($"{nameof(GameContentCatalogSO)} '{name}' has no reward cards.");
        }

        if (Collections.Count == 0)
        {
            errors.Add($"{nameof(GameContentCatalogSO)} '{name}' has no collections.");
        }

        if (Enemies.Count == 0)
        {
            errors.Add($"{nameof(GameContentCatalogSO)} '{name}' has no enemies.");
        }

        if (DefaultCharacter == null)
        {
            errors.Add($"{nameof(GameContentCatalogSO)} '{name}' is missing default character.");
        }

        ValidateNoDuplicates(Weapons, "weapon", errors);
        ValidateNoDuplicates(Accessories, "accessory", errors);
        ValidateNoDuplicates(RewardCards, "reward card", errors);
        ValidateNoDuplicates(StarterCards, "starter card", errors);
        ValidateNoDuplicates(Collections, "collection", errors);
        ValidateNoDuplicates(Enemies, "enemy", errors);
        ValidateNoDuplicates(Buffs, "buff", errors);
        return errors.Count == initialCount;
    }

    private void OnValidate()
    {
        rewardCards ??= System.Array.Empty<RewardCardSO>();
        starterCards ??= System.Array.Empty<RewardCardSO>();
        collections ??= System.Array.Empty<CollectionSO>();
        enemies ??= System.Array.Empty<EnemySO>();
        buffs ??= System.Array.Empty<BuffDataSO>();

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

    private static void ValidateNoDuplicates<T>(IReadOnlyList<T> items, string label, List<string> errors)
        where T : UnityEngine.Object
    {
        if (items == null || items.Count == 0)
        {
            return;
        }

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
