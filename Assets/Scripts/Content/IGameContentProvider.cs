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
    CharacterDataSO DefaultCharacter { get; }
    IReadOnlyList<RewardCardSO> StarterCards { get; }
    Player DefaultPlayerPrefab { get; }
    Weapon DefaultWeaponPrefab { get; }
    PlayerLevelConfigSO PlayerLevelConfig { get; }
    RunProgressionProfileSO RunProgressionProfile { get; }
    ContentPoolSO UpgradeCardPool { get; }
    ContentPoolSO ChestRewardPool { get; }
    ContentPoolSO ShopPool { get; }
    ContentPoolSO DropPool { get; }
    ContentPoolSO WaveSpawnPool { get; }
    ContentPoolSO WeaponRewardPool { get; }
    PropPresentationCatalogSO PropPresentationCatalog { get; }
    StageDefinitionSO DefaultStageDefinition { get; }
    DamageTextFlow DamageTextPrefab { get; }
    DamageTextVisualConfigSO DamageTextVisualConfig { get; }
    Material ItemQualityIconEffectMaterial { get; }
}
