using UnityEngine;

/// <summary>
/// 运行时装配器：
/// - 安装角色数据提供的运行时特性；
/// - 发放初始武器与初始饰品；
/// - 作为“进入游戏时的一次性装配入口”，避免把 loadout 逻辑耦合进 PropertiesManager。
/// 当前数据源仍然是 CharacterDataSO，但组件职责已经收敛为更通用的 loadout 安装。
/// </summary>
[RequireComponent(typeof(FeatureHost), typeof(AccessoryManager), typeof(WeaponsHolder))]
public class LoadoutInstaller : MonoBehaviour
{
    [SerializeField] private CharacterDataSO characterData;

    private FeatureHost featureHost;
    private WeaponsHolder weaponsHolder;
    private AccessoryManager accessoryManager;
    private Entity entity;
    private bool hasInstalledLoadout;

    public CharacterDataSO LoadoutSource => characterData;

    private void Awake()
    {
        featureHost = GetComponent<FeatureHost>();
        weaponsHolder = GetComponent<WeaponsHolder>();
        accessoryManager = GetComponent<AccessoryManager>();
        entity = GetComponent<Entity>();
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
    }

    private void OnGameStateChanged(GameStateChangedEvent eventData)
    {
        if (eventData.NewState != GameState.Game || hasInstalledLoadout)
        {
            return;
        }

        CharacterDataSO selectedCharacter = CharacterSelectionManager.Instance != null
            ? CharacterSelectionManager.Instance.SelectedCharacter
            : null;
        if (selectedCharacter != null)
        {
            characterData = selectedCharacter;
        }

        ApplyRenderedCharacterIcon();
        InstallRuntimeFeatures();
        ApplyInitialLoadout();
        hasInstalledLoadout = true;
    }

    private void ApplyRenderedCharacterIcon()
    {
        if (characterData == null)
        {
            return;
        }

        entity?.EntityRenderer?.SetSprite(characterData.CharacterIcon);
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

        for (int i = 0; i < characterData.InitialWeapons.Count; i++)
        {
            WeaponLevelEntry entry = characterData.InitialWeapons[i];
            if (entry.weaponData == null)
            {
                continue;
            }

            weaponsHolder.AddWeapon(entry.weaponData, entry.level);
        }

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
