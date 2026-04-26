using UnityEngine;

/// <summary>
/// 移动策略基类。
/// 这里只描述“如何移动”的空间行为，例如追击、绕圈、拉扯、后撤等。
/// 不应该在策略内部直接修改实体基础属性、状态开关或临时战斗数值；
/// 若某个移动状态需要移速/攻速等联动，请在实体配置中声明，并由状态机在 OnEnter / OnExit
/// 通过属性管理器做成对增减，保证进入与退出都可恢复，避免固定值覆盖原始配置。
/// </summary>
public abstract class MovementStrategyBase : ScriptableObject
{
    public abstract void ExecuteMove(IMovable movable, Entity self, Entity target, EnemySO enemyData);
}
