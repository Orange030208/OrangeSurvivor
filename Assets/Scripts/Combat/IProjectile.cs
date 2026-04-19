using System;

/// <summary>
/// 投射物运行时接口。
/// 用于把发射逻辑与具体 Projectile MonoBehaviour 解耦。
/// // 扩展说明：后续若需要统一暴露命中回调、回收或初始化阶段，可继续在这里补充接口能力。
/// </summary>
public interface IProjectile : IEntity
{
    void Launch(ProjectileLaunchContext context);
}
