using System;
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
public class Player : Entity, IPropGroupProvider
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

    public override IMovable MoveComponent => playerController;
    public override Vector2 Center => (Vector2)transform.position + collider.offset;

    public Rigidbody2D Rb => rb;
    public PropertiesManager PropertiesManager => propertiesManager;

    public CharacterDataSO CharacterData => characterData;
    public BasePropGroupSO BasePropsGroup => characterData.BasePropsAsset;

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
        OnEnableComponent();
    }

    private void Update()
    {
        Tick();
    }

    private void FixedUpdate()
    {
        FixedTick();
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        FeatureInstaller.RemoveSource(featureHost, FeatureInstaller.CharacterSourceId);
        hasInstalledLoadout = false;
        OnDisableComponent();
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
        ApplyInitialLoadout();
        hasInstalledLoadout = true;
    }

    private void InstallRuntimeFeatures()
    {
        if (characterData == null || featureHost == null)
        {
            return;
        }

        FeatureInstaller.InstallCharacter(featureHost, characterData);
    }

    private void ApplyInitialLoadout()
    {
        if (characterData == null)
        {
            return;
        }

        if (weaponsHolder != null)
        {
            for (int i = 0; i < characterData.InitialWeapons.Count; i++)
            {
                WeaponEntry entry = characterData.InitialWeapons[i];
                if (entry.weaponData == null)
                {
                    continue;
                }

                weaponsHolder.AddWeapon(entry.weaponData, entry.level);
            }
        }

        if (accessoryManager != null)
        {
            for (int i = 0; i < characterData.InitialAccessories.Count; i++)
            {
                AccessoryDataSO accessory = characterData.InitialAccessories[i];
                if (accessory == null)
                {
                    continue;
                }

                accessoryManager.EquipAccessory(accessory);
            }
        }
    }
}