using UnityEngine;

/// <summary>
/// 武器序列调试信息构建器：
/// - 只负责把序列资源 + 武器当前运行时参数，转换成调试面板可读数据；
/// - 避免 Weapon 基类混入仅服务于编辑器检查的职责。
/// 扩展说明：后续如需输出更多编辑器/运行时调试指标，优先在这里补充，不要继续膨胀 Weapon 基类。
/// </summary>
public static class WeaponSequenceDebugInfoBuilder
{
    public static WeaponSequenceDebugInfo Build(Weapon weapon, AttackSequenceDefinitionSO sequence)
    {
        if (weapon == null || sequence == null)
        {
            return new WeaponSequenceDebugInfo(0f, 0f, 0f);
        }

        float attackInterval = Mathf.Max(0.01f, weapon.RuntimeStats.AttackInterval);
        float occupancy = weapon.WeaponData != null ? weapon.WeaponData.AttackSequenceOccupancy : 0.85f;
        float timingWindowDuration = attackInterval * occupancy;
        float effectiveDuration = Mathf.Min(Mathf.Max(0.01f, sequence.Duration), Mathf.Max(0.01f, timingWindowDuration));
        return new WeaponSequenceDebugInfo(sequence.Duration, effectiveDuration, timingWindowDuration);
    }
}
