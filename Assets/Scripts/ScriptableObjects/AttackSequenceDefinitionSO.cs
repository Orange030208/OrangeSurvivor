using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 定义一套武器攻击序列：动作采样帧、重定向设置与玩法事件。
/// </summary>
[CreateAssetMenu(fileName = "Weapon Attack Sequence", menuName = ScriptableObjectMenuPaths.WEAPON_ATTACK_SEQUENCE, order = 0)]
public class AttackSequenceDefinitionSO : ScriptableObject
{
    [Header("检视面板")]
    [Tooltip("一次完整攻击序列的持续时间。动作和事件关键帧使用 0 到 1 的归一化时间。")]
    [SerializeField] private float duration = 0.25f;
    [Tooltip("播放完成后，动画目标是否回到缓存的默认姿态。")]
    [SerializeField] private bool restoreDefaultPoseOnComplete = true;
    [Tooltip("该序列开始时如何解析运行时目标偏移。")]
    [SerializeField] private WeaponSequenceTargetOffsetMode targetOffsetMode = WeaponSequenceTargetOffsetMode.ActualTarget;
    [Tooltip("制作该动画时参考的目标本地偏移。真实目标偏移等于该值时，动作采样会原样播放。")]
    [SerializeField] private Vector2 referenceTargetOffset = new(0f, 1f);
    [Tooltip("各轴重定向缩放权重。0 保留原始采样，1 表示该轴完全缩放到当前目标偏移。")]
    [SerializeField] private Vector2 retargetScaleWeight = new(0f, 1f);
    [Tooltip("动作采样朝参考目标反方向移动时使用的各轴倍率。0 保留原始后撤蓄力，1 使用正常重定向权重。")]
    [SerializeField] private Vector2 oppositeDirectionRetargetWeight = new(1f, 0f);

    [Header("运动")]
    [Tooltip("攻击动作采样得到的本地位置与旋转帧。")]
    [SerializeField] private List<WeaponMotionKeyframe> motionKeyframes = new()
    {
        new WeaponMotionKeyframe(0f, Vector3.zero, Vector3.zero),
        new WeaponMotionKeyframe(0.35f, new Vector3(0f, 0.18f, 0f), new Vector3(0f, 0f, -30f), WeaponMotionEase.InSine),
        new WeaponMotionKeyframe(0.55f, new Vector3(0f, 0.28f, 0f), new Vector3(0f, 0f, 40f), WeaponMotionEase.OutSine),
        new WeaponMotionKeyframe(1f, Vector3.zero, Vector3.zero, WeaponMotionEase.InOutSine)
    };

    [Header("序列事件")]
    [Tooltip("序列播放过程中触发的玩法事件与表现事件。")]
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
    public WeaponSequenceTargetOffsetMode TargetOffsetMode => targetOffsetMode;
    public Vector2 ReferenceTargetOffset => referenceTargetOffset;
    public Vector2 RetargetScaleWeight => new(Mathf.Clamp01(retargetScaleWeight.x), Mathf.Clamp01(retargetScaleWeight.y));
    public Vector2 OppositeDirectionRetargetWeight => new(Mathf.Clamp01(oppositeDirectionRetargetWeight.x), Mathf.Clamp01(oppositeDirectionRetargetWeight.y));
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

    public void ConfigureRetargeting(Vector2 referenceOffset, Vector2 scaleWeight)
    {
        ConfigureRetargeting(referenceOffset, scaleWeight, oppositeDirectionRetargetWeight);
    }

    public void ConfigureRetargeting(Vector2 referenceOffset, Vector2 scaleWeight, Vector2 oppositeDirectionWeight)
    {
        referenceTargetOffset = referenceOffset;
        retargetScaleWeight = new Vector2(Mathf.Clamp01(scaleWeight.x), Mathf.Clamp01(scaleWeight.y));
        oppositeDirectionRetargetWeight = new Vector2(Mathf.Clamp01(oppositeDirectionWeight.x), Mathf.Clamp01(oppositeDirectionWeight.y));
    }

    public void ConfigureTargetOffsetMode(WeaponSequenceTargetOffsetMode mode)
    {
        targetOffsetMode = mode;
    }
}

public enum WeaponSequenceTargetOffsetMode
{
    ActualTarget,
    MaxRangeAlongAimDirection
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

[Serializable]
public struct WeaponMotionKeyframe
{
    [Range(0f, 1f)] public float normalizedTime;
    public float localPositionX;
    public float localPositionY;
    public Vector3 localEulerAngles;
    public WeaponMotionEase ease;
    public AnimationCurve customCurve;

    public WeaponMotionKeyframe(float normalizedTime, Vector3 localPosition, Vector3 localEulerAngles, WeaponMotionEase ease = WeaponMotionEase.Linear)
    {
        this.normalizedTime = Mathf.Clamp01(normalizedTime);
        localPositionX = localPosition.x;
        localPositionY = localPosition.y;
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
