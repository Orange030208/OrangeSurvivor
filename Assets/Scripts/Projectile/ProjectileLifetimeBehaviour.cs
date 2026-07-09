using UnityEngine;

/// <summary>
/// 弹射物生命周期模块基类，只判断“是否该结束”，不直接销毁对象。
/// 结束后的爆炸或普通消失交给命中模块决定。
/// </summary>
public abstract class ProjectileLifetimeBehaviour : MonoBehaviour
{
    protected ProjectileRuntimeContext RuntimeContext { get; private set; }

    public virtual void Initialize(in ProjectileRuntimeContext context)
    {
        RuntimeContext = context;
    }

    public abstract void ResetState();
    public abstract ProjectileLifetimeResult Tick(float deltaTime);
}
