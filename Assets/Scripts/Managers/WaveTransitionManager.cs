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

    private void OnEnable()
    {
        GameEventBus.Subscribe<RequestUpgradeOptionsSnapshotEvent>(PublishSnapshot);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<RequestUpgradeOptionsSnapshotEvent>(PublishSnapshot);
    }

    public void BeforeGameStateChanged(GameState oldState, GameState newState)
    {
    }

    public void AfterGameStateChanged(GameState oldState, GameState newState)
    {
        switch (newState)
        {
            case GameState.WaveTransition:
                ConfigureUpgradeProps();
                break;
        }
    }

    [NaughtyAttributes.Button]
    private void ConfigureUpgradeProps()
    {
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
        return () => propsManager.AddProp(temp.propType, temp.value);
    }

    private void PublishSnapshot()
    {
        GameEventBus.Publish(new UpgradeOptionsChangedEvent(UpgradeProps));
    }
}

public struct UpgradeProp
{
    public PropType propType;
    public float value;
    public Action upgradeBonusCallback;
}
