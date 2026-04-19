using UnityEngine;

/// <summary>
/// 武器序列调试数据快照：
/// - 提供编辑器所需的序列时长观测值；
/// - 让 Weapon 基类不再暴露仅供调试使用的公开 API。
/// 扩展说明：后续若要补充更多序列调试指标，优先扩展该快照结构，不要把调试接口继续堆回 Weapon。
/// </summary>
public readonly struct WeaponSequenceDebugInfo
{
    public float OriginalDuration { get; }
    public float EffectiveDuration { get; }
    public float TimingWindowDuration { get; }
    public float CompressionRatio { get; }

    public WeaponSequenceDebugInfo(float originalDuration, float effectiveDuration, float timingWindowDuration)
    {
        OriginalDuration = Mathf.Max(0f, originalDuration);
        EffectiveDuration = Mathf.Max(0f, effectiveDuration);
        TimingWindowDuration = Mathf.Max(0f, timingWindowDuration);
        CompressionRatio = OriginalDuration <= 0.0001f
            ? 1f
            : EffectiveDuration / OriginalDuration;
    }
}
