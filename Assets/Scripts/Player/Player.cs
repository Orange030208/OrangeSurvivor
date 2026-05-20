using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(PlayerLevel))]
[RequireComponent(typeof(BuffController))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(FeatureHost))]
[RequireComponent(typeof(AccessoryManager))]
[RequireComponent(typeof(WeaponsHolder))]
[RequireComponent(typeof(PlayerAnimationController))]
[RequireComponent(typeof(CurrencyWallet))]
[RequireComponent(typeof(PropertiesManager))]
public class Player : Entity, IPropGroupProvider,IPropModifierProvider, IInitialWeaponProvider, IInitialAccessoryProvider,IFeatureEffectsProvider
{
    [Header("组件")]
    private Rigidbody2D rb;
    private PlayerController playerController;
    private PropertiesManager propertiesManager;
    [SerializeField] private CharacterDataSO characterData;

    public override IMovable MoveComponent => playerController;
    public Rigidbody2D Rb => rb;
    public PropertiesManager PropertiesManager => propertiesManager;

    public CharacterDataSO CharacterData => characterData;
    public BasePropGroupSO BasePropsGroup => characterData != null ? characterData.BasePropsAsset : null;
    public IReadOnlyList<WeaponEntry> InitialWeapons => characterData != null
        ? characterData.InitialWeapons
        : System.Array.Empty<WeaponEntry>();

    public IReadOnlyList<AccessoryDataSO> InitialAccessories => characterData != null
        ? characterData.InitialAccessories
        : System.Array.Empty<AccessoryDataSO>();
    
    public IReadOnlyList<PropModifierData> PropModifierDataList => characterData != null
        ? characterData.ExtraProps
        : System.Array.Empty<PropModifierData>();

    public IReadOnlyList<FeatureBase> FeatureEffects => characterData != null
        ? characterData.SpecialFeatures
        : System.Array.Empty<FeatureBase>();

    private void Awake()
    {
        ResolveDefaultCharacter();
        InitComponentReferences();
    }

    private void Start()
    {
        ResolveDefaultCharacter();
        InitializeComponent();
        EnableAllComponents();
    }

    private void Update()
    {
        TickAllComponents();
    }

    private void FixedUpdate()
    {
        FixedTickAllComponents();
    }

    private void OnDisable()
    {
        DisableAllComponents();
    }

    private void InitComponentReferences()
    {
        rb = GetComponent<Rigidbody2D>();
        propertiesManager = GetComponent<PropertiesManager>();
        playerController = GetComponent<PlayerController>();
    }

    private void ResolveDefaultCharacter()
    {
        if (characterData != null)
        {
            return;
        }

        if (!GameContentRuntime.TryGetProvider(out IGameContentProvider provider))
        {
            return;
        }

        characterData = provider.DefaultCharacter;
        if (characterData == null)
        {
            Debug.LogError($"{nameof(Player)} '{name}' requires {nameof(GameContentCatalogSO)} default character.", this);
        }
    }
}
