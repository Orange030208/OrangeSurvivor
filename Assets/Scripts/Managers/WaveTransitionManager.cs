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
            UpgradeProps[i].prop = (EntityProps)Random.Range(0, Enum.GetNames(typeof(EntityProps)).Length);

            // 这里将“属性对应的执行逻辑”与“展示数值”解耦：
            // actionToPerform 负责真正执行升级，upgradeProp.value 负责提供 UI 展示值。
            Action actionToPerform = GetActionToPerform(UpgradeProps[i].prop, out UpgradeProp upgradeProp);
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

    private Action GetActionToPerform(EntityProps prop, out UpgradeProp upgradeProp)
    {
        upgradeProp = new UpgradeProp();
        upgradeProp.prop = prop;
        upgradeProp.value = 0;

        // TODO:AI写或者策略模式扩展
        // 这里负责给“不同属性”分配对应的展示值。
        // 当前仅实现 Attack，后续新增属性时在此补齐数值策略即可。
        switch (prop)
        {
            case EntityProps.Attack:
                upgradeProp.value = Random.Range(1, 5);
                break;
        }

        // 返回延迟执行动作：按钮点击时才真正生效。
        // 当前逻辑仅打印日志，后续应替换为对应属性的实际加成实现。
        return () => print($"处理{prop.ToString()}");
    }
}

public struct UpgradeProp
{
    public EntityProps prop;
    public float value;
    public Action upgradeBonusCallback;
}
