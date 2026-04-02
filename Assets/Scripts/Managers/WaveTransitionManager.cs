using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

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
            // 在 EntityProps 全枚举范围内随机生成一个可选升级属性。
            UpgradeProps[i].propType = (EntityPropType)Random.Range(0, Enum.GetNames(typeof(EntityPropType)).Length);

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

    private Action GetActionToPerform(EntityPropType propType, out UpgradeProp upgradeProp)
    {
        upgradeProp = new UpgradeProp();
        upgradeProp.propType = propType;
        upgradeProp.value = 0;

        // TODO:AI写或者策略模式扩展
        switch (propType)
        {
            case EntityPropType.Attack:
                upgradeProp.value = Random.Range(1, 5);
                break;
            case EntityPropType.MaxHealth:
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
    public EntityPropType propType;
    public float value;
    public Action upgradeBonusCallback;
}
