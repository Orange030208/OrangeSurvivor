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
    public IReadOnlyList<RewardCardSO> RewardCards => catalog.RewardCards;
    public IReadOnlyList<CollectionSO> Collections => catalog.Collections;
    public IReadOnlyList<EnemySO> Enemies => catalog.Enemies;
    public IReadOnlyList<BuffDataSO> Buffs => catalog.Buffs;
    public CharacterDataSO DefaultCharacter => catalog.DefaultCharacter;
    public IReadOnlyList<RewardCardSO> StarterCards => catalog.StarterCards;
    public Player DefaultPlayerPrefab => catalog.DefaultPlayerPrefab;
    public Weapon DefaultWeaponPrefab => catalog.DefaultWeaponPrefab;
    public PlayerLevelConfigSO PlayerLevelConfig => catalog.PlayerLevelConfig;
    public RunProgressionProfileSO RunProgressionProfile => catalog.RunProgressionProfile;
    public DropCollectionProfileSO DropCollectionProfile => catalog.DropCollectionProfile;
    public StageDirectorProfileSO DefaultStageDirectorProfile => catalog.DefaultStageDirectorProfile;
    public PropPresentationCatalogSO PropPresentationCatalog => catalog.PropPresentationCatalog;
    public DamageTextFlow DamageTextPrefab => catalog.DamageTextPrefab;
    public DamageTextVisualConfigSO DamageTextVisualConfig => catalog.DamageTextVisualConfig;
    public Material ItemQualityIconEffectMaterial => catalog.ItemQualityIconEffectMaterial;
    public TierColorPaletteSO TierColorPalette => catalog.TierColorPalette;
    public ContentTierWeightProfileSO ContentTierWeightProfile => catalog.ContentTierWeightProfile;
}
