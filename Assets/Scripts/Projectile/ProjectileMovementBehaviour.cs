using UnityEngine;

/// <summary>
/// 弹射物运动模块基类，只负责改变弹体位置或刚体速度。
/// 命中、伤害和销毁由其他模块处理，避免运动类型和玩法效果互相污染。
/// </summary>
public abstract class ProjectileMovementBehaviour : MonoBehaviour
{
    protected ProjectileRuntimeContext RuntimeContext { get; private set; }

    public virtual void Initialize(in ProjectileRuntimeContext context)
    {
        RuntimeContext = context;
    }

    public abstract void Launch();

    public virtual void Tick(float deltaTime)
    {
    }

    public abstract void Stop();
}
