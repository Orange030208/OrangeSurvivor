/// <summary>
/// 武器命中完成事件：
/// - 在一次武器命中伤害成功应用后广播；
/// - 供独立系统订阅，而不是反向依赖 Weapon 具体实现；
/// - 命名遵循完成态语义，表示伤害已经结算完成。
/// </summary>
public struct WeaponHitCompletedEvent : IGameEvent
{
    public Weapon Weapon;
    public HitResult HitResult;

    public WeaponHitCompletedEvent(Weapon weapon, HitResult hitResult)
    {
        Weapon = weapon;
        HitResult = hitResult;
    }
}
