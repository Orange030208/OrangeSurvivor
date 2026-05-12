using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 将 <see cref="GameContentCatalogSO"/> 适配成 <see cref="IGameContentProvider"/> 的轻量包装。
/// 这里不做兜底加载、不做缓存策略、不做随机逻辑，保证 Catalog 是运行时内容的唯一来源。
/// </summary>
public sealed class GameContentCatalogProvider : IGameContentProvider
{
    private readonly GameContentCatalogSO catalog;

    /// <summary>
    /// 基于一个显式 Catalog 引用创建 Provider。
    /// </summary>
    public GameContentCatalogProvider(GameContentCatalogSO catalog)
    {
        this.catalog = catalog != null
            ? catalog
            : throw new ArgumentNullException(nameof(catalog), $"{nameof(GameContentCatalogProvider)} requires a catalog.");
    }

    public IReadOnlyList<WeaponDataSO> Weapons => catalog.Weapons;
    public IReadOnlyList<AccessoryDataSO> Accessories => catalog.Accessories;
    public IReadOnlyList<CharacterDataSO> Characters => catalog.Characters;
    public Player DefaultPlayerPrefab => catalog.DefaultPlayerPrefab;
    public Weapon DefaultWeaponPrefab => catalog.DefaultWeaponPrefab;
    public PlayerLevelConfigSO PlayerLevelConfig => catalog.PlayerLevelConfig;
    public RunProgressionProfileSO RunProgressionProfile => catalog.RunProgressionProfile;
    public ContentPoolSO UpgradeCardPool => catalog.UpgradeCardPool;
    public ContentPoolSO ChestRewardPool => catalog.ChestRewardPool;
    public ContentPoolSO ShopPool => catalog.ShopPool;
    public ContentPoolSO DropPool => catalog.DropPool;
    public ContentPoolSO WaveSpawnPool => catalog.WaveSpawnPool;
    public ContentPoolSO WeaponRewardPool => catalog.WeaponRewardPool;
    public CardQualityPresentationCatalogSO CardQualityPresentationCatalog => catalog.CardQualityPresentationCatalog;
    public PropPresentationCatalogSO PropPresentationCatalog => catalog.PropPresentationCatalog;
    public StageDefinitionSO DefaultStageDefinition => catalog.DefaultStageDefinition;
    public DamageTextFlow DamageTextPrefab => catalog.DamageTextPrefab;
    public DamageTextVisualConfigSO DamageTextVisualConfig => catalog.DamageTextVisualConfig;
    public ItemQualityVisualConfigSO ItemQualityVisualConfig => catalog.ItemQualityVisualConfig;
    public Material ItemQualityIconEffectMaterial => catalog.ItemQualityIconEffectMaterial;
}
