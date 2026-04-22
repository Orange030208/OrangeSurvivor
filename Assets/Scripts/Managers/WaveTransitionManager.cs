using System;
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
    [SerializeField] private Player player;
    [SerializeField] private PropertiesManager propertiesManager;
    [SerializeField] private CurrencyWallet currencyWallet;

    private readonly PropEntry[] propEntries = new PropEntry[3];
    private AccessoryDataSO currentAccessoryData;
    private int collectChestCount;
    private PlayerLevel playerLevel;
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

    private void OnEnable()
    {
        GameEventBus.Subscribe<RequestWaveTransitionStateSnapshotEvent>(PublishSnapshot);
        GameEventBus.Subscribe<AccessoryOperateEvent>(OnAccessoryOperated);
        GameEventBus.Subscribe<UpgradeContainerClickedEvent>(OnUpgradeContainerClicked);
        GameEventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        GameEventBus.Subscribe<ChestCollectedEvent>(OnChestCollected);
        GameEventBus.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);

        TryBindPlayerReferences();
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<RequestWaveTransitionStateSnapshotEvent>(PublishSnapshot);
        GameEventBus.Unsubscribe<AccessoryOperateEvent>(OnAccessoryOperated);
        GameEventBus.Unsubscribe<UpgradeContainerClickedEvent>(OnUpgradeContainerClicked);
        GameEventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        GameEventBus.Unsubscribe<ChestCollectedEvent>(OnChestCollected);
        GameEventBus.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
    }

    private void OnGameStateChanged(GameStateChangedEvent eventData)
    {
        if (eventData.NewState == GameState.WaveTransition)
        {
            StartTransitionFlow();
            return;
        }

        if (eventData.OldState == GameState.WaveTransition)
        {
            currentAccessoryData = null;
            CurrentPhase = TransitionPhase.None;
        }
    }

    private void OnChestCollected()
    {
        collectChestCount++;
    }

    private void OnPlayerSpawned(PlayerSpawnedEvent eventData)
    {
        player = eventData.Player;
        accessoryManager = player.GetComponent<AccessoryManager>();
        propertiesManager = player.GetComponent<PropertiesManager>();
        playerLevel = player.GetComponent<PlayerLevel>();
        currencyWallet = player.GetComponent<CurrencyWallet>();
    }

    private void StartTransitionFlow()
    {
        currentAccessoryData = null;
        CurrentPhase = TransitionPhase.None;
        TryBindPlayerReferences();
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
            currencyWallet?.ChangeAmount(eventData.accessoryData.RecyclePrice);
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
            PropType propType = (PropType)Random.Range(0, Enum.GetNames(typeof(PropType)).Length);
            PropModifierType modifierType = GetRandomModifierTypeForProp(propType);
            propEntries[i] = new PropEntry(propType, modifierType, GetRandomValueFor(propType, modifierType));
        }

        GameEventBus.Publish(new UpgradeOptionsChangedEvent(propEntries));
    }

    private PropModifierType GetRandomModifierTypeForProp(PropType propType)
    {
        return propType switch
        {
            PropType.Attack or PropType.MaxHealth or PropType.Armor => Random.value > 0.5f
                ? PropModifierType.Flat
                : PropModifierType.BasePercent,
            PropType.AttackSpeed or PropType.CriticalChance or PropType.CriticalPercent or PropType.Range =>
                Random.value > 0.5f ? PropModifierType.BasePercent : PropModifierType.FinalPercent,
            _ => PropModifierType.Flat
        };
    }

    private float GetRandomValueFor(PropType propType, PropModifierType modifierType)
    {
        return modifierType switch
        {
            PropModifierType.Flat => GetRandomFlatValue(propType),
            PropModifierType.FinalFlat => GetRandomFlatValue(propType),
            PropModifierType.BasePercent => Random.Range(0.05f, 0.2f),
            PropModifierType.FinalPercent => Random.Range(0.05f, 0.15f),
            _ => 0f
        };
    }

    private float GetRandomFlatValue(PropType propType)
    {
        return propType switch
        {
            PropType.Attack => Random.Range(2f, 6f),
            PropType.MaxHealth => Random.Range(10f, 30f),
            PropType.MoveSpeed => Random.Range(0.5f, 2f),
            _ => Random.Range(1f, 3f)
        };
    }

    private void CompleteUpgradeSelection()
    {
        CurrentPhase = TransitionPhase.None;
        GameEventBus.Publish<UpgradeSelectionCompletedEvent>();
    }

    private void ContinueOrCompleteUpgradeSelection()
    {
        if (CurrentPhase != TransitionPhase.UpgradeSelection)
        {
            return;
        }

        int remainingUpgradePoints = playerLevel.ConsumeUpgradePoint();
        if (remainingUpgradePoints > 0)
        {
            ConfigureUpgradeProps();
            return;
        }

        CompleteUpgradeSelection();
    }

    private void OnUpgradeContainerClicked(UpgradeContainerClickedEvent eventData)
    {
        if (CurrentPhase != TransitionPhase.UpgradeSelection)
        {
            return;
        }

        string upgradeId = $"Upgrade_{Guid.NewGuid():N}";
        PropEntry propEntry = propEntries[eventData.ContainerIndex];
        propertiesManager.AddModifier(upgradeId, propEntry);

        ContinueOrCompleteUpgradeSelection();
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

    private void TryBindPlayerReferences()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
        }

        if (player == null)
        {
            accessoryManager = null;
            propertiesManager = null;
            playerLevel = null;
            currencyWallet = null;
            return;
        }

        if (accessoryManager == null)
        {
            accessoryManager = player.GetComponent<AccessoryManager>();
        }

        if (propertiesManager == null)
        {
            propertiesManager = player.GetComponent<PropertiesManager>();
        }

        if (playerLevel == null)
        {
            playerLevel = player.GetComponent<PlayerLevel>();
        }

        if (currencyWallet == null)
        {
            currencyWallet = player.GetComponent<CurrencyWallet>();
        }

        if (currencyWallet == null)
        {
            currencyWallet = player.GetComponent<CurrencyWallet>();
        }
    }
}