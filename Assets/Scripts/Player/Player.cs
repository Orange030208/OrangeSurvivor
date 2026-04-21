using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(PlayerLevel))]
[RequireComponent(typeof(BuffController))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(FeatureHost))]
[RequireComponent(typeof(AccessoryManager))]
[RequireComponent(typeof(WeaponsHolder))]
[RequireComponent(typeof(PlayerAnimationController))]
public class Player : Entity
{
    [Header("组件")]
    [SerializeField] private new CircleCollider2D collider;
    [SerializeField] private EntityRenderer entityRenderer;
    private PlayerLevel playerLevel;
    private PlayerController playerController;
    private FeatureHost featureHost;
    private WeaponsHolder weaponsHolder;
    private AccessoryManager accessoryManager;
    private PlayerAnimationController playerAnimationController;
    private bool hasInstalledLoadout;
    public override IMovement MoveComponent => playerController;
    public override Vector2 Center => (Vector2)transform.position + collider.offset;
    public override EntityRenderer EntityRenderer => entityRenderer;

    private CharacterDataSO characterData;
    private void Awake()
    {
        playerLevel = GetComponent<PlayerLevel>();
        playerController = GetComponent<PlayerController>();
        featureHost = GetComponent<FeatureHost>();
        weaponsHolder = GetComponent<WeaponsHolder>();
        accessoryManager = GetComponent<AccessoryManager>();
        playerAnimationController =  GetComponent<PlayerAnimationController>();
        if (entityRenderer == null)
        {
            entityRenderer = GetComponentInChildren<EntityRenderer>();
        }
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        if (featureHost != null)
        {
            FeatureInstaller.RemoveSource(featureHost, FeatureInstaller.CharacterSourceId);
        }

        hasInstalledLoadout = false;
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

        ApplyCharacterAnimator();
        InstallRuntimeFeatures();
        ApplyInitialLoadout();
        hasInstalledLoadout = true;
    }

    private void ApplyCharacterAnimator()
    {
        playerAnimationController.Animator.runtimeAnimatorController = characterData.CharacterAnimatorController;
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
                WeaponLevelEntry entry = characterData.InitialWeapons[i];
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