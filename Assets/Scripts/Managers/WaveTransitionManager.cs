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
public class WaveTransitionManager : MonoSingletonBase<WaveTransitionManager>, IGameStateListener
{
    //TODO:后续修改掉
    [SerializeField] private AccessoryManager accessoryManager;

    private readonly UpgradeProp[] upgradeProps = new UpgradeProp[3];
    private AccessoryDataSO currentAccessoryData;
    private int _collectChestCount;
    private TransitionPhase _currentPhase = TransitionPhase.None;

    public TransitionPhase CurrentPhase
    {
        get => _currentPhase;
        private set
        {
            if (_currentPhase == value)
            {
                return;
            }

            TransitionPhase oldPhase = _currentPhase;
            _currentPhase = value;
            GameEventBus.Publish(new WaveTransitionPhaseChanged(oldPhase, _currentPhase));
        }
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<WaveTransitionSnapshot>(PublishSnapshot);
        GameEventBus.Subscribe<AccessoryOperateEvent>(OnAccessoryOperated);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<WaveTransitionSnapshot>(PublishSnapshot);
        GameEventBus.Unsubscribe<AccessoryOperateEvent>(OnAccessoryOperated);
    }

    public void BeforeGameStateChanged(GameState oldState, GameState newState)
    {
    }

    public void AfterGameStateChanged(GameState oldState, GameState newState)
    {
        if (newState == GameState.WaveTransition)
        {
            StartTransitionFlow();
        }
    }

    private void StartTransitionFlow()
    {
        currentAccessoryData = null;
        CurrentPhase = TransitionPhase.None;
        TryEnterNextPhase();
    }

    private void TryEnterNextPhase()
    {
        if (_collectChestCount > 0)
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

    /// <summary>
    /// 玩家对饰品做完选择后的回调
    /// </summary>
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
            accessoryManager.EquipAccessory(eventData.accessoryData);
            print($"选择了{eventData.accessoryData.DisplayName}");
        }
        else
        {
            CurrencyManager.Instance.AddCurrency(eventData.accessoryData.RecyclePrice);
            print($"回收了{eventData.accessoryData.DisplayName},回收价格:{eventData.accessoryData.RecyclePrice}");
        }

        _collectChestCount = Mathf.Max(0, _collectChestCount - 1);
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

        for (int i = 0; i < upgradeProps.Length; i++)
        {
            upgradeProps[i].propType = (PropType)Random.Range(0, Enum.GetNames(typeof(PropType)).Length);

            Action actionToPerform = GetActionToPerform(upgradeProps[i].propType, out UpgradeProp upgradeProp);
            upgradeProps[i].value = upgradeProp.value;

            upgradeProps[i].upgradeBonusCallback = null;
            upgradeProps[i].upgradeBonusCallback += actionToPerform;
            upgradeProps[i].upgradeBonusCallback += UpgradeBonusCallback;
        }

        GameEventBus.Publish(new UpgradeOptionsChangedEvent(upgradeProps));
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
            GameManager.Instance.EnterShop();
        }
    }

    private Action GetActionToPerform(PropType propType, out UpgradeProp upgradeProp)
    {
        upgradeProp = new UpgradeProp
        {
            propType = propType,
            value = 0
        };

        switch (propType)
        {
            case PropType.Attack:
                upgradeProp.value = Random.Range(1, 5);
                break;
            case PropType.MaxHealth:
                upgradeProp.value = Random.Range(1, 5);
                break;
        }

        PropertiesManager propsManager = FindObjectOfType<PropertiesManager>();
        var temp = upgradeProp;
        string upgradeId = $"Upgrade_{Guid.NewGuid():N}";
        return () => propsManager.AddBonusModifier(upgradeId, temp.propType, temp.value);
    }

    private void PublishSnapshot()
    {
        GameEventBus.Publish(new WaveTransitionPhaseChanged(TransitionPhase.None, CurrentPhase));

        switch (CurrentPhase)
        {
            case TransitionPhase.ChestSelection:
                if (currentAccessoryData != null)
                {
                    GameEventBus.Publish(new AccessorySelectionStartedEvent(currentAccessoryData));
                }
                break;
            case TransitionPhase.UpgradeSelection:
                GameEventBus.Publish(new UpgradeOptionsChangedEvent(upgradeProps));
                break;
        }
    }

    //TODO:后续修改掉
    public void CollectChest()
    {
        _collectChestCount++;
    }
}

public struct UpgradeProp
{
    public PropType propType;
    public float value;
    public Action upgradeBonusCallback;
}
