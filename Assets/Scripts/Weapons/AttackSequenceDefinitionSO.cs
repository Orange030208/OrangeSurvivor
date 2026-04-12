using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻击序列资源：
/// - <see cref="motionKeyframes"/> 定义武器在一次攻击中的位移/旋转轨迹；
/// - <see cref="eventKeyframes"/> 定义命中窗口、发射弹射物、播放特效等逻辑事件；
/// - WeaponSequenceBridge + WeaponMotionSequencePlayer 会在运行时读取这份数据并驱动武器。
/// </summary>
[CreateAssetMenu(fileName = "Weapon Attack Sequence", menuName = "SO/WeaponAttackSequence", order = 0)]
public class AttackSequenceDefinitionSO : ScriptableObject
{
    [Header("Inspector")]
    [Tooltip("一次完整攻击序列的总时长。所有关键帧时间都是基于 0~1 的归一化时间，再乘以这个时长得到实际秒数。")]
    [SerializeField] private float duration = 0.25f;
    [Tooltip("序列播放完成后，是否自动恢复到初始待机姿态。大部分武器建议保持开启。")]
    [SerializeField] private bool restoreDefaultPoseOnComplete = true;

    [Header("Motion")]
    [Tooltip("动作关键帧列表：定义这次攻击中武器如何位移、如何旋转。")]
    [SerializeField] private List<WeaponMotionKeyframe> motionKeyframes = new()
    {
        new WeaponMotionKeyframe(0f, Vector3.zero, Vector3.zero),
        new WeaponMotionKeyframe(0.35f, new Vector3(0f, 0.18f, 0f), new Vector3(0f, 0f, -30f), WeaponMotionEase.InSine),
        new WeaponMotionKeyframe(0.55f, new Vector3(0f, 0.28f, 0f), new Vector3(0f, 0f, 40f), WeaponMotionEase.OutSine),
        new WeaponMotionKeyframe(1f, Vector3.zero, Vector3.zero, WeaponMotionEase.InOutSine)
    };

    [Header("Events")]
    [Tooltip("事件关键帧列表：在攻击过程中的某些时间点触发命中窗口、发射、SFX、VFX 等逻辑。")]
    [SerializeField] private List<WeaponSequenceEventKeyframe> eventKeyframes = new()
    {
        WeaponSequenceEventKeyframe.CreateWindowEvent(0.4f, WeaponSequenceEventType.OpenHitWindow, 0),
        WeaponSequenceEventKeyframe.CreateWindowEvent(0.62f, WeaponSequenceEventType.CloseHitWindow, 0),
        WeaponSequenceEventKeyframe.CreateProjectileEvent(0.5f, new ProjectileSpawnPayload(0, null, 0, ProjectileFiringMode.Default, ProjectilePatternConfig.Default)),
        WeaponSequenceEventKeyframe.CreateSimpleEvent(0.5f, WeaponSequenceEventType.PlaySfx),
        WeaponSequenceEventKeyframe.CreateSimpleEvent(0.5f, WeaponSequenceEventType.PlayVfx)
    };

    public float Duration => Mathf.Max(0.01f, duration);
    public bool RestoreDefaultPoseOnComplete => restoreDefaultPoseOnComplete;
    public IReadOnlyList<WeaponMotionKeyframe> MotionKeyframes => motionKeyframes;
    public IReadOnlyList<WeaponSequenceEventKeyframe> EventKeyframes => eventKeyframes;

    public static AttackSequenceDefinitionSO CreateRuntimeSequence(string sequenceName, float sequenceDuration)
    {
        AttackSequenceDefinitionSO sequence = CreateInstance<AttackSequenceDefinitionSO>();
        sequence.name = sequenceName;
        sequence.hideFlags = HideFlags.HideAndDontSave;
        sequence.duration = sequenceDuration;
        sequence.restoreDefaultPoseOnComplete = true;
        return sequence;
    }

    public void Overwrite(float sequenceDuration, bool restorePose, IReadOnlyList<WeaponMotionKeyframe> motions, IReadOnlyList<WeaponSequenceEventKeyframe> events)
    {
        duration = Mathf.Max(0.01f, sequenceDuration);
        restoreDefaultPoseOnComplete = restorePose;
        motionKeyframes = motions != null ? new List<WeaponMotionKeyframe>(motions) : new List<WeaponMotionKeyframe>();
        eventKeyframes = events != null ? new List<WeaponSequenceEventKeyframe>(events) : new List<WeaponSequenceEventKeyframe>();
    }
}

public enum WeaponMotionEase
{
    Linear,
    InSine,
    OutSine,
    InOutSine,
    InQuad,
    OutQuad,
    InOutQuad,
    InCubic,
    OutCubic,
    InOutCubic,
    InExpo,
    OutExpo,
    InOutExpo,
    OutBack,
    OutElastic,
    CustomCurve
}

public enum WeaponMotionPositionMode
{
    Fixed,
    DynamicFromTarget
}

public enum WeaponMotionDynamicPositionStrategy
{
    None,
    /// <summary>
    /// 朝目标方向，并把这帧落点限制在 dynamicMinNormalizedReach ~ dynamicMaxNormalizedReach 对应的真实攻击半径之间。
    /// 例如最大值为 1 时，表示这一帧最多打到整把武器的攻击半径边界。
    /// </summary>
    TowardTargetClampedRadius
}

[Serializable]
public struct WeaponMotionKeyframe
{
    [Range(0f, 1f)] public float normalizedTime;
    public WeaponMotionPositionMode positionMode;
    public Vector3 localPosition;
    public WeaponMotionDynamicPositionStrategy dynamicPositionStrategy;
    [Range(0f, 1f)] public float dynamicMinNormalizedReach;
    [Range(0f, 1f)] public float dynamicMaxNormalizedReach;
    public Vector3 localEulerAngles;
    public WeaponMotionEase ease;
    public AnimationCurve customCurve;

    public WeaponMotionKeyframe(float normalizedTime, Vector3 localPosition, Vector3 localEulerAngles, WeaponMotionEase ease = WeaponMotionEase.Linear)
    {
        this.normalizedTime = Mathf.Clamp01(normalizedTime);
        positionMode = WeaponMotionPositionMode.Fixed;
        // 固定帧按武器本地空间中的写死坐标解释，不受 RuntimeStats.Range 影响。
        this.localPosition = localPosition;
        dynamicPositionStrategy = WeaponMotionDynamicPositionStrategy.None;
        // 固定帧与动态帧都改用“归一化攻击空间”表达：
        // 1 = 恰好到达当前武器的攻击半径边界；
        // 0.5 = 到达一半攻击半径；
        // 这样运行时只需要乘 RuntimeStats.Range，就能统一得到真实落点。
        dynamicMinNormalizedReach = Mathf.Clamp01(localPosition.magnitude * 0.35f);
        dynamicMaxNormalizedReach = Mathf.Clamp01(localPosition.magnitude);
        this.localEulerAngles = localEulerAngles;
        this.ease = ease;
        customCurve = ease == WeaponMotionEase.CustomCurve ? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f) : null;
    }
}

public enum WeaponSequenceEventType
{
    OpenHitWindow,
    CloseHitWindow,
    SpawnProjectile,
    PlaySfx,
    PlayVfx
}

[Serializable]
public struct WeaponSequenceEventKeyframe
{
    [Range(0f, 1f)] public float normalizedTime;
    public WeaponSequenceEventType eventType;
    public int windowId;
    public ProjectileSpawnPayload projectileSpawnPayload;

    public static WeaponSequenceEventKeyframe CreateWindowEvent(float normalizedTime, WeaponSequenceEventType eventType, int windowId)
    {
        return new WeaponSequenceEventKeyframe
        {
            normalizedTime = Mathf.Clamp01(normalizedTime),
            eventType = eventType,
            windowId = Mathf.Max(0, windowId),
            projectileSpawnPayload = ProjectileSpawnPayload.Default
        };
    }

    public static WeaponSequenceEventKeyframe CreateProjectileEvent(float normalizedTime, ProjectileSpawnPayload projectileSpawnPayload)
    {
        return new WeaponSequenceEventKeyframe
        {
            normalizedTime = Mathf.Clamp01(normalizedTime),
            eventType = WeaponSequenceEventType.SpawnProjectile,
            windowId = 0,
            projectileSpawnPayload = projectileSpawnPayload
        };
    }

    public static WeaponSequenceEventKeyframe CreateSimpleEvent(float normalizedTime, WeaponSequenceEventType eventType)
    {
        return new WeaponSequenceEventKeyframe
        {
            normalizedTime = Mathf.Clamp01(normalizedTime),
            eventType = eventType,
            windowId = 0,
            projectileSpawnPayload = ProjectileSpawnPayload.Default
        };
    }
}
