using System;
using Survivors.Player;
using UnityEngine;
using Random = UnityEngine.Random;

public enum TransitionPhase
{
    None,
    ChestSelection,
    UpgradeSelection
}

/// <summary>
/// 波次过渡管理器，负责在波次之间提供玩家属性升级选项。
/// </summary>
public class WaveTransitionManager : MonoBehaviour
{
    [SerializeField] private AccessoryManager accessoryManager;

    private readonly PropEntry[] propEntries = new PropEntry[3];
    private AccessoryDataSO currentAccessoryData;
    private int collectChestCount;
    private TransitionPhase currentPhase = TransitionPhase.None;

    private TransitionPhase CurrentPhase
    {
        get => currentPhase;
        set
        {
            if (currentPhase == value)
            {
                return;
            }

            TransitionPhase oldPhase = currentPhase;
            currentPhase = value;
            GameEventBus.Publish(new WaveTransitionPhaseChangedEvent(oldPhase, currentPhase));
        }
    }

    private void Awake()
    {
        if (accessoryManager == null)
        {
            Player player = FindFirstObjectByType<Player>();
            accessoryManager = player != null ? player.GetComponent<AccessoryManager>() : null;
        }
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<RequestWaveTransitionStateSnapshotEvent>(PublishSnapshot);
        GameEventBus.Subscribe<AccessoryOperateEvent>(OnAccessoryOperated);
        GameEventBus.Subscribe<UpgradeContainerClickedEvent>(OnUpgradeContainerClicked);
        GameEventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        GameEventBus.Subscribe<ChestCollectedEvent>(OnChestCollected);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<RequestWaveTransitionStateSnapshotEvent>(PublishSnapshot);
        GameEventBus.Unsubscribe<AccessoryOperateEvent>(OnAccessoryOperated);
        GameEventBus.Unsubscribe<UpgradeContainerClickedEvent>(OnUpgradeContainerClicked);
        GameEventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        GameEventBus.Unsubscribe<ChestCollectedEvent>(OnChestCollected);
    }

    private void OnGameStateChanged(GameStateChangedEvent eventData)
    {
        if (eventData.NewState == GameState.WaveTransition)
        {
            StartTransitionFlow();
        }
    }

    private void OnChestCollected()
    {
        collectChestCount++;
    }

    private void StartTransitionFlow()
    {
        currentAccessoryData = null;
        CurrentPhase = TransitionPhase.None;
        TryEnterNextPhase();
    }

    private void TryEnterNextPhase()
    {
        if (collectChestCount > 0)
        {
            EnterChestSelection();
            return;
        }

        EnterUpgradeSelection();
    }

    private void EnterChestSelection()
    {
        CurrentPhase = TransitionPhase.ChestSelection;
        currentAccessoryData = ResourcesManager.GetRandomAccessory();
        GameEventBus.Publish(new AccessorySelectionStartedEvent(currentAccessoryData));
    }

    private void OnAccessoryOperated(AccessoryOperateEvent eventData)
    {
        if (CurrentPhase != TransitionPhase.ChestSelection)
        {
            return;
        }

        if (currentAccessoryData == null || eventData.accessoryData != currentAccessoryData)
        {
            return;
        }

        if (eventData.selected)
        {
            accessoryManager?.EquipAccessory(eventData.accessoryData);
            print($"选择了{eventData.accessoryData.ItemName}");
        }
        else
        {
            GameEventBus.Publish(new CurrencyChangeRequestedEvent(CurrencyType.Currency, eventData.accessoryData.RecyclePrice));
            print($"回收了{eventData.accessoryData.ItemName},回收价格:{eventData.accessoryData.RecyclePrice}");
        }

        collectChestCount = Mathf.Max(0, collectChestCount - 1);
        currentAccessoryData = null;
        TryEnterNextPhase();
    }

    private void EnterUpgradeSelection()
    {
        currentAccessoryData = null;
        CurrentPhase = TransitionPhase.UpgradeSelection;
        ConfigureUpgradeProps();
    }

    [NaughtyAttributes.Button]
    private void ConfigureUpgradeProps()
    {
        if (CurrentPhase != TransitionPhase.UpgradeSelection)
        {
            return;
        }

        for (int i = 0; i < propEntries.Length; i++)
        {
            propEntries[i].propType = (PropType)Random.Range(0, Enum.GetNames(typeof(PropType)).Length);
            propEntries[i].value = GetRandomValueForPropType(propEntries[i].propType);
        }

        GameEventBus.Publish(new UpgradeOptionsChangedEvent(propEntries));
    }

    private float GetRandomValueForPropType(PropType propType)
    {
        switch (propType)
        {
            case PropType.Attack:
            case PropType.MaxHealth:
                return Random.Range(1, 5);
            default:
                return Random.Range(1, 3);
        }
    }

    private void UpgradeBonusCallback()
    {
        if (CurrentPhase != TransitionPhase.UpgradeSelection)
        {
            return;
        }

        Player player = FindFirstObjectByType<Player>();
        if (player.UseUpgradePoints() > 0)
        {
            ConfigureUpgradeProps();
        }
        else
        {
            CurrentPhase = TransitionPhase.None;
            GameEventBus.Publish<UpgradeSelectionCompletedEvent>();
        }
    }

    private void OnUpgradeContainerClicked(UpgradeContainerClickedEvent eventData)
    {
        if (CurrentPhase != TransitionPhase.UpgradeSelection)
        {
            return;
        }

        PropertiesManager propsManager = FindObjectOfType<PropertiesManager>();
        if (propsManager != null)
        {
            string upgradeId = $"Upgrade_{Guid.NewGuid():N}";
            PropEntry propEntry = propEntries[eventData.ContainerIndex];
            propsManager.AddBonusModifier(upgradeId, propEntry.propType, propEntry.value);
        }

        UpgradeBonusCallback();
    }

    private void PublishSnapshot()
    {
        GameEventBus.Publish(new WaveTransitionPhaseChangedEvent(TransitionPhase.None, CurrentPhase));

        switch (CurrentPhase)
        {
            case TransitionPhase.ChestSelection:
                if (currentAccessoryData != null)
                {
                    GameEventBus.Publish(new AccessorySelectionStartedEvent(currentAccessoryData));
                }
                break;
            case TransitionPhase.UpgradeSelection:
                GameEventBus.Publish(new UpgradeOptionsChangedEvent(propEntries));
                break;
        }
    }
}
