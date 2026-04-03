using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/// <summary>
/// 波次过渡管理器，负责在波次之间提供玩家属性升级选项。
/// 架构模式：
/// 1. 单例模式（MonoSingletonBase）确保全局唯一实例
/// 2. 观察者模式（IGameStateListener）监听游戏状态变化
/// 3. 策略模式（Strategy Pattern）雏形：通过Action回调实现不同属性的升级逻辑
/// 4. 发布-订阅模式：通过OnUpdatePropsChanged事件通知UI更新
/// </summary>
public class WaveTransitionManager : MonoSingletonBase<WaveTransitionManager>,IGameStateListener
{
    public UpgradeProp[] UpgradeProps { private set; get; } = new UpgradeProp[3];
    
    public event Action<UpgradeProp[]> OnUpdatePropsChanged;

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
            // 在 PropType 全枚举范围内随机生成一个可选升级属性。
            UpgradeProps[i].propType = (PropType)Random.Range(0, Enum.GetNames(typeof(PropType)).Length);

            // 这里将“属性对应的执行逻辑”与“展示数值”解耦：
            // actionToPerform 负责真正执行升级，upgradeProp.value 负责提供 UI 展示值。
            Action actionToPerform = GetActionToPerform(UpgradeProps[i].propType, out UpgradeProp upgradeProp);
            UpgradeProps[i].value = upgradeProp.value;

            // 每轮重建选项前清空旧回调，避免同一个槽位重复叠加导致一次点击触发多次。
            UpgradeProps[i].upgradeBonusCallback = null;
            UpgradeProps[i].upgradeBonusCallback += actionToPerform;

            // 统一补充“升级后流程”回调：扣点 -> 继续刷新/进入商店。
            UpgradeProps[i].upgradeBonusCallback += UpgradeBonusCallback;
        }

        // 将最新三项候选升级推送给 UI 层。
        OnUpdatePropsChanged?.Invoke(UpgradeProps);
    }

    private void UpgradeBonusCallback()
    {
        // TODO:暂时这样写，框架修改时一起重写
        // 该流程依赖场景内唯一 Player：
        // - 有剩余升级点：继续生成下一轮候选升级；
        // - 无升级点：结束过渡，进入商店阶段。
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

    /// <summary>
    /// 获取属性对应的升级执行逻辑（策略模式实现点）
    /// 每个属性类型可以有不同的数值计算方式和执行逻辑
    /// 当前为简化实现，仅对Attack和MaxHealth属性设置随机值
    /// </summary>
    private Action GetActionToPerform(PropType propType, out UpgradeProp upgradeProp)
    {
        upgradeProp = new UpgradeProp();
        upgradeProp.propType = propType;
        upgradeProp.value = 0;

        // TODO:AI写或者策略模式扩展
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
}

public struct UpgradeProp
{
    public PropType propType;
    public float value;
    public Action upgradeBonusCallback;
}
