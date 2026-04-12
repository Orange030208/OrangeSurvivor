using UnityEngine;

/// <summary>
/// 一次伤害结算的数据载体。
/// 当前只包含：
/// - damage：最终伤害值；
/// - position：用于飘字、受击特效、击中反馈的位置；
/// - isCritical：是否暴击。
/// 这是一个很基础的结构，后续如果要加伤害类型、来源实体、击退方向等，也可以继续扩展。
/// </summary>
public struct DamageInfo
{
    public float damage;
    public Vector2 position; 
    public bool isCritical;

    public DamageInfo(float damage, Vector2 position, bool isCritical)
    {
        this.damage = damage;
        this.position = position;
        this.isCritical = isCritical;
    }
}
