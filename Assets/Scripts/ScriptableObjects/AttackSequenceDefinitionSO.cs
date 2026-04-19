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

    [Header("Sequence Events")]
    [Tooltip("事件关键帧列表：在攻击过程中的某些时间点触发命中窗口、发射、SFX、VFX 等逻辑事件。")]
    [SerializeField] private List<WeaponSequenceEventKeyframe> eventKeyframes = new()
    {
        WeaponSequenceEventKeyframe.CreateWindowEvent(0.4f, WeaponSequenceEventType.OpenHitWindow, 0),
        WeaponSequenceEventKeyframe.CreateWindowEvent(0.62f, WeaponSequenceEventType.CloseHitWindow, 0),
        WeaponSequenceEventKeyframe.CreateSimpleEvent(0.5f, WeaponSequenceEventType.SpawnProjectile, 0),
        WeaponSequenceEventKeyframe.CreateSimpleEvent(0.5f, WeaponSequenceEventType.PlaySfx, 0),
        WeaponSequenceEventKeyframe.CreateSimpleEvent(0.5f, WeaponSequenceEventType.PlayVfx, 0)
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
    /// 按目标相关位置解算动态轴，并把结果限制在当前轴自己的 Min/Max Reach 对应的真实攻击半径范围内。
    /// </summary>
    TowardTargetClampedRadius
}

[Serializable]
public struct WeaponMotionKeyframe
{
    [Range(0f, 1f)] public float normalizedTime;
    public WeaponMotionPositionMode xPositionMode;
    public WeaponMotionPositionMode yPositionMode;
    public WeaponMotionDynamicPositionStrategy dynamicPositionStrategy;
    public float localPositionX;
    [Range(0f, 1f)] public float xDynamicMinNormalizedReach;
    [Range(0f, 1f)] public float xDynamicMaxNormalizedReach;
    public float localPositionY;
    [Range(0f, 1f)] public float yDynamicMinNormalizedReach;
    [Range(0f, 1f)] public float yDynamicMaxNormalizedReach;
    public Vector3 localEulerAngles;
    public WeaponMotionEase ease;
    public AnimationCurve customCurve;

    public WeaponMotionKeyframe(float normalizedTime, Vector3 localPosition, Vector3 localEulerAngles, WeaponMotionEase ease = WeaponMotionEase.Linear)
    {
        this.normalizedTime = Mathf.Clamp01(normalizedTime);
        xPositionMode = WeaponMotionPositionMode.Fixed;
        yPositionMode = WeaponMotionPositionMode.Fixed;
        localPositionX = localPosition.x;
        localPositionY = localPosition.y;
        dynamicPositionStrategy = WeaponMotionDynamicPositionStrategy.None;
        xDynamicMinNormalizedReach = Mathf.Clamp01(Mathf.Abs(localPosition.x) * 0.35f);
        xDynamicMaxNormalizedReach = Mathf.Clamp01(Mathf.Abs(localPosition.x));
        yDynamicMinNormalizedReach = Mathf.Clamp01(Mathf.Abs(localPosition.y) * 0.35f);
        yDynamicMaxNormalizedReach = Mathf.Clamp01(Mathf.Abs(localPosition.y));
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
    public int eventKey;

    public static WeaponSequenceEventKeyframe CreateWindowEvent(float normalizedTime, WeaponSequenceEventType eventType, int eventKey)
    {
        return new WeaponSequenceEventKeyframe
        {
            normalizedTime = Mathf.Clamp01(normalizedTime),
            eventType = eventType,
            eventKey = Mathf.Max(0, eventKey)
        };
    }

    public static WeaponSequenceEventKeyframe CreateSimpleEvent(float normalizedTime, WeaponSequenceEventType eventType, int eventKey = 0)
    {
        return new WeaponSequenceEventKeyframe
        {
            normalizedTime = Mathf.Clamp01(normalizedTime),
            eventType = eventType,
            eventKey = Mathf.Max(0, eventKey)
        };
    }
}
