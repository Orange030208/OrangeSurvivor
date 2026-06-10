using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 运行时内容的只读访问接口。
/// Provider 只暴露已经显式引用的资源，不负责路径加载，也不负责随机抽取。
/// </summary>
public interface IGameContentProvider
{
    IReadOnlyList<WeaponDataSO> Weapons { get; }
    IReadOnlyList<AccessoryDataSO> Accessories { get; }
    IReadOnlyList<RewardCardSO> RewardCards { get; }
    IReadOnlyList<CollectionSO> Collections { get; }
    IReadOnlyList<EnemySO> Enemies { get; }
    IReadOnlyList<BuffDataSO> Buffs { get; }
    CharacterDataSO DefaultCharacter { get; }
    IReadOnlyList<RewardCardSO> StarterCards { get; }
    Player DefaultPlayerPrefab { get; }
    Weapon DefaultWeaponPrefab { get; }
    PlayerLevelConfigSO PlayerLevelConfig { get; }
    RunProgressionProfileSO RunProgressionProfile { get; }
    DropCollectionProfileSO DropCollectionProfile { get; }
    StageDirectorProfileSO DefaultStageDirectorProfile { get; }
    PropPresentationCatalogSO PropPresentationCatalog { get; }
    DamageTextFlow DamageTextPrefab { get; }
    DamageTextVisualConfigSO DamageTextVisualConfig { get; }
    Material ItemQualityIconEffectMaterial { get; }
    TierColorPaletteSO TierColorPalette { get; }
    ContentTierWeightProfileSO ContentTierWeightProfile { get; }
}
