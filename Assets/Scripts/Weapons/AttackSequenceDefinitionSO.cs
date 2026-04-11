using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Weapon Attack Sequence", menuName = "SO/WeaponAttackSequence", order = 0)]
public class AttackSequenceDefinitionSO : ScriptableObject
{
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private bool restoreDefaultPoseOnComplete = true;
    [SerializeField] private List<WeaponMotionKeyframe> motionKeyframes = new()
    {
        new WeaponMotionKeyframe(0f, Vector3.zero, Vector3.zero),
        new WeaponMotionKeyframe(0.35f, new Vector3(0f, 0.18f, 0f), new Vector3(0f, 0f, -30f), WeaponMotionEase.InSine),
        new WeaponMotionKeyframe(0.55f, new Vector3(0f, 0.28f, 0f), new Vector3(0f, 0f, 40f), WeaponMotionEase.OutSine),
        new WeaponMotionKeyframe(1f, Vector3.zero, Vector3.zero, WeaponMotionEase.InOutSine)
    };
    [SerializeField] private List<WeaponSequenceEventKeyframe> eventKeyframes = new()
    {
        WeaponSequenceEventKeyframe.CreateWindowEvent(0.4f, WeaponSequenceEventType.OpenHitWindow, 0),
        WeaponSequenceEventKeyframe.CreateWindowEvent(0.62f, WeaponSequenceEventType.CloseHitWindow, 0),
        WeaponSequenceEventKeyframe.CreateProjectileEvent(0.5f, new ProjectileSpawnPayload(0, 0, 0, ProjectileFiringMode.Default)),
        WeaponSequenceEventKeyframe.CreateSimpleEvent(0.5f, WeaponSequenceEventType.PlaySfx),
        WeaponSequenceEventKeyframe.CreateSimpleEvent(0.5f, WeaponSequenceEventType.PlayVfx)
    };

    public float Duration => Mathf.Max(0.01f, duration);
    public bool RestoreDefaultPoseOnComplete => restoreDefaultPoseOnComplete;
    public IReadOnlyList<WeaponMotionKeyframe> MotionKeyframes => motionKeyframes;
    public IReadOnlyList<WeaponSequenceEventKeyframe> EventKeyframes => eventKeyframes;
}

public enum WeaponMotionEase
{
    Linear,
    InSine,
    OutSine,
    InOutSine,
    OutBack,
    CustomCurve
}

[Serializable]
public struct WeaponMotionKeyframe
{
    [Range(0f, 1f)] public float normalizedTime;
    public Vector3 localPosition;
    public Vector3 localEulerAngles;
    public WeaponMotionEase ease;
    public AnimationCurve customCurve;

    public WeaponMotionKeyframe(float normalizedTime, Vector3 localPosition, Vector3 localEulerAngles, WeaponMotionEase ease = WeaponMotionEase.Linear)
    {
        this.normalizedTime = Mathf.Clamp01(normalizedTime);
        this.localPosition = localPosition;
        this.localEulerAngles = localEulerAngles;
        this.ease = ease;
        customCurve = ease == WeaponMotionEase.CustomCurve
            ? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f)
            : null;
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
