/// <summary>
/// 武器攻击执行器接口。
/// 目的是把“武器决定何时攻击”和“具体如何把这次攻击执行出去”拆开：
/// - 近战执行器负责碰撞检测和伤害结算；
/// - 远程执行器负责生成投射物。
/// 当前接口很轻量，只有最基础的一次 ExecuteAttack；
/// 如果后续需要统一支持取消、预热、回调等，再考虑扩展接口，而不是过早抽象。
/// </summary>
public interface IWeaponAttackExecutor
{
    void ExecuteAttack(in WeaponAttackContext context);
}
