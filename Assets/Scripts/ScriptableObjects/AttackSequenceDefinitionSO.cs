using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines one weapon attack sequence: sampled motion frames, retarget settings, and gameplay events.
/// </summary>
[CreateAssetMenu(fileName = "Weapon Attack Sequence", menuName = ScriptableObjectMenuPaths.WEAPON_ATTACK_SEQUENCE, order = 0)]
public class AttackSequenceDefinitionSO : ScriptableObject
{
    [Header("Inspector")]
    [Tooltip("Duration of one full attack sequence. Motion and event keyframes use normalized time from 0 to 1.")]
    [SerializeField] private float duration = 0.25f;
    [Tooltip("Whether the animated transform returns to its cached default pose when playback completes.")]
    [SerializeField] private bool restoreDefaultPoseOnComplete = true;
    [Tooltip("How the runtime target offset is resolved when this sequence starts.")]
    [SerializeField] private WeaponSequenceTargetOffsetMode targetOffsetMode = WeaponSequenceTargetOffsetMode.ActualTarget;
    [Tooltip("The target local offset this animation was authored against. When the real target offset equals this value, motion samples play unchanged.")]
    [SerializeField] private Vector2 referenceTargetOffset = new(0f, 1f);
    [Tooltip("Per-axis retarget scale weight. 0 keeps the authored sample, 1 fully scales that axis toward the current target offset.")]
    [SerializeField] private Vector2 retargetScaleWeight = new(0f, 1f);
    [Tooltip("Per-axis multiplier used when a motion sample moves opposite to the reference target direction. 0 keeps backward windup authored, 1 keeps the regular retarget weight.")]
    [SerializeField] private Vector2 oppositeDirectionRetargetWeight = new(1f, 0f);

    [Header("Motion")]
    [Tooltip("Sampled local position and rotation frames for the attack motion.")]
    [SerializeField] private List<WeaponMotionKeyframe> motionKeyframes = new()
    {
        new WeaponMotionKeyframe(0f, Vector3.zero, Vector3.zero),
        new WeaponMotionKeyframe(0.35f, new Vector3(0f, 0.18f, 0f), new Vector3(0f, 0f, -30f), WeaponMotionEase.InSine),
        new WeaponMotionKeyframe(0.55f, new Vector3(0f, 0.28f, 0f), new Vector3(0f, 0f, 40f), WeaponMotionEase.OutSine),
        new WeaponMotionKeyframe(1f, Vector3.zero, Vector3.zero, WeaponMotionEase.InOutSine)
    };

    [Header("Sequence Events")]
    [Tooltip("Gameplay and presentation events triggered during sequence playback.")]
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
