using System;
using UnityEngine;

/// <summary>
/// 投射物发射者接口。
/// 用于把“谁负责生成并发射 Projectile”抽象成统一协作边界。
/// // 扩展说明：后续可新增批量发射、预热发射或对象池发射实现，而不修改调用方。
/// </summary>
public interface IProjectileLauncher
{
    void LaunchProjectile(IProjectile projectile, in ProjectileLaunchContext context);
}
