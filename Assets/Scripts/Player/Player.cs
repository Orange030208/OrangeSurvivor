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
public class Player : Entity, IPropGroupProvider , IInitialWeaponProvider,IInitialAccessoryProvider
{
    [Header("组件")]
    [SerializeField] private new CircleCollider2D collider;
    private Rigidbody2D rb;
    private PlayerLevel playerLevel;
    private PlayerController playerController;
    private FeatureHost featureHost;
    private WeaponsHolder weaponsHolder;
    private AccessoryManager accessoryManager;
    private PlayerAnimationController playerAnimationController;
    private PropertiesManager propertiesManager;
    private CharacterDataSO characterData;

    private bool hasInstalledLoadout;
    private IReadOnlyList<AccessoryDataSO> initialWeapons;

    public override IMovable MoveComponent => playerController;
    public override Vector2 Center => (Vector2)transform.position + collider.offset;

    public Rigidbody2D Rb => rb;
    public PropertiesManager PropertiesManager => propertiesManager;

    public CharacterDataSO CharacterData => characterData;
    public BasePropGroupSO BasePropsGroup => characterData.BasePropsAsset;
    public IReadOnlyList<WeaponEntry> InitialWeapons => characterData.InitialWeapons;

    public IReadOnlyList<AccessoryDataSO> InitialAccessories => characterData.InitialAccessories;
    
    private void Awake()
    {
        InitComponentReferences();
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    private void Start()
    {
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
        GameEventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        FeatureInstaller.RemoveSource(featureHost, FeatureInstaller.CharacterSourceId);
        hasInstalledLoadout = false;
        DisableAllComponents();
    }

    private void InitComponentReferences()
    {
        rb = GetComponent<Rigidbody2D>();
        playerLevel = GetComponent<PlayerLevel>();
        propertiesManager = GetComponent<PropertiesManager>();
        playerController = GetComponent<PlayerController>();
        featureHost = GetComponent<FeatureHost>();
        weaponsHolder = GetComponent<WeaponsHolder>();
        accessoryManager = GetComponent<AccessoryManager>();
        playerAnimationController = GetComponent<PlayerAnimationController>();
    }

    private void OnGameStateChanged(GameStateChangedEvent eventData)
    {
        if (eventData.NewState != GameState.Game || hasInstalledLoadout)
        {
            return;
        }

        CharacterDataSO selectedCharacter = CharacterSelectionManager.Instance.SelectedCharacter;
        if (selectedCharacter != null)
        {
            characterData = selectedCharacter;
        }

        InstallRuntimeFeatures();
        hasInstalledLoadout = true;
    }

    private void InstallRuntimeFeatures()
    {
        FeatureInstaller.InstallCharacter(featureHost, characterData);
    }
}