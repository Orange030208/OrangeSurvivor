using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/// <summary>
/// 波次过渡管理器，负责在波次之间提供玩家属性升级选项。
/// </summary>
public class WaveTransitionManager : MonoSingletonBase<WaveTransitionManager>, IGameStateListener
{
    public UpgradeProp[] UpgradeProps { private set; get; } = new UpgradeProp[3];

    private int collectChestCount = 0;
    private TransitionPhase _currentPhase = TransitionPhase.None;

    private enum TransitionPhase
    {
        None,
        ChestSelection,
        UpgradeSelection
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<RequestUpgradeOptionsSnapshotEvent>(PublishSnapshot);
        GameEventBus.Subscribe<ChestSelectionCompletedEvent>(OnChestSelectionCompleted);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<RequestUpgradeOptionsSnapshotEvent>(PublishSnapshot);
        GameEventBus.Unsubscribe<ChestSelectionCompletedEvent>(OnChestSelectionCompleted);
    }

    public void BeforeGameStateChanged(GameState oldState, GameState newState)
    {
    }

    public void AfterGameStateChanged(GameState oldState, GameState newState)
    {
        switch (newState)
        {
            case GameState.WaveTransition:
                StartTransitionFlow();
                break;
        }
    }

    private void StartTransitionFlow()
    {
        _currentPhase = TransitionPhase.ChestSelection;
        TryOpenChest();
    }

    private void TryOpenChest()
    {
        if (collectChestCount > 0)
        {
            ShowAccessory();
        }
        else
        {
            ProceedToUpgradeSelection();
        }
    }

    private void ShowAccessory()
    {
        _currentPhase = TransitionPhase.ChestSelection;
        GameEventBus.Publish(new ChestSelectionStartedEvent(collectChestCount));
    }

    private void OnChestSelectionCompleted(ChestSelectionCompletedEvent e)
    {
        collectChestCount = 0;
        ProceedToUpgradeSelection();
    }

    private void ProceedToUpgradeSelection()
    {
        _currentPhase = TransitionPhase.UpgradeSelection;
        ConfigureUpgradeProps();
    }

    [NaughtyAttributes.Button]
    private void ConfigureUpgradeProps()
    {
        if (_currentPhase != TransitionPhase.UpgradeSelection)
        {
            return;
        }

        for (int i = 0; i < UpgradeProps.Length; i++)
        {
            UpgradeProps[i].propType = (PropType)Random.Range(0, Enum.GetNames(typeof(PropType)).Length);

            Action actionToPerform = GetActionToPerform(UpgradeProps[i].propType, out UpgradeProp upgradeProp);
            UpgradeProps[i].value = upgradeProp.value;

            UpgradeProps[i].upgradeBonusCallback = null;
            UpgradeProps[i].upgradeBonusCallback += actionToPerform;
            UpgradeProps[i].upgradeBonusCallback += UpgradeBonusCallback;
        }

        GameEventBus.Publish(new UpgradeOptionsChangedEvent(UpgradeProps));
    }

    private void UpgradeBonusCallback()
    {
        Player player = FindFirstObjectByType<Player>();
        if (player.UseUpgradePoints() > 0)
        {
            ConfigureUpgradeProps();
        }
        else
        {
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
        return () => propsManager.AddAdditiveModifier(upgradeId, temp.propType, temp.value);
    }

    private void PublishSnapshot()
    {
        GameEventBus.Publish(new UpgradeOptionsChangedEvent(UpgradeProps));
    }

    //TODO:后续修改掉
    public void CollectChest()
    {
        collectChestCount++;
    }
}

public struct UpgradeProp
{
    public PropType propType;
    public float value;
    public Action upgradeBonusCallback;
}
