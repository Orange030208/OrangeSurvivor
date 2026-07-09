using System;
using System.Collections.Generic;
using Orange.GameServices;
using UnityEngine;

/// <summary>
/// 场景服务化的运行时内容入口。
/// 场景服务化的运行时内容入口。
/// </summary>
[Serializable]
public sealed class ContentService : GameService, IGameContentProvider
{
    [SerializeField] private GameContentCatalogSO catalog;

    private GameContentCatalogProvider provider;

    public GameContentCatalogSO Catalog => catalog;

    public IReadOnlyList<WeaponDataSO> Weapons => Provider.Weapons;
    public IReadOnlyList<AccessoryDataSO> Accessories => Provider.Accessories;
    public IReadOnlyList<RewardCardSO> RewardCards => Provider.RewardCards;
    public IReadOnlyList<CollectionSO> Collections => Provider.Collections;
    public IReadOnlyList<EnemySO> Enemies => Provider.Enemies;
    public IReadOnlyList<BuffDataSO> Buffs => Provider.Buffs;
    public CharacterDataSO DefaultCharacter => Provider.DefaultCharacter;
    public IReadOnlyList<RewardCardSO> StarterCards => Provider.StarterCards;
    public Player DefaultPlayerPrefab => Provider.DefaultPlayerPrefab;
    public Weapon DefaultWeaponPrefab => Provider.DefaultWeaponPrefab;
    public PlayerLevelConfigSO PlayerLevelConfig => Provider.PlayerLevelConfig;
    public RunProgressionProfileSO RunProgressionProfile => Provider.RunProgressionProfile;
    public DropCollectionProfileSO DropCollectionProfile => Provider.DropCollectionProfile;
    public StageDirectorProfileSO DefaultStageDirectorProfile => Provider.DefaultStageDirectorProfile;
    public PropPresentationCatalogSO PropPresentationCatalog => Provider.PropPresentationCatalog;
    public DamageTextFlow DamageTextPrefab => Provider.DamageTextPrefab;
    public DamageTextVisualConfigSO DamageTextVisualConfig => Provider.DamageTextVisualConfig;
    public Material ItemQualityIconEffectMaterial => Provider.ItemQualityIconEffectMaterial;
    public TierColorPaletteSO TierColorPalette => Provider.TierColorPalette;
    public ContentTierWeightProfileSO ContentTierWeightProfile => Provider.ContentTierWeightProfile;

    private GameContentCatalogProvider Provider =>
        provider ?? throw new InvalidOperationException($"{nameof(ContentService)} has not been attached.");

    protected override void RegisterContracts(GameServiceRegistry registry)
    {
        registry.Register<IGameContentProvider>(this);
    }

    protected override void OnValidateService(GameServiceValidationReport report)
    {
        if (catalog == null)
        {
            report.AddError($"{nameof(ContentService)} requires a {nameof(GameContentCatalogSO)}.", GetType());
            return;
        }

        List<string> errors = new();
        if (!catalog.ValidateCatalog(errors))
        {
            for (int i = 0; i < errors.Count; i++)
            {
                report.AddError(errors[i], GetType());
            }
        }
    }

    protected override void OnAttach()
    {
        if (catalog == null)
        {
            throw new MissingReferenceException($"{nameof(ContentService)} requires a {nameof(GameContentCatalogSO)}.");
        }

        provider = new GameContentCatalogProvider(catalog);
        GameContentRuntime.SetProvider(this);
    }

    protected override void OnDispose()
    {
        GameContentRuntime.ClearProvider(this);
        provider = null;
    }
}
