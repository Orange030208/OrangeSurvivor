using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 近战武器：负责在命中窗口内持续做碰撞检测，并把命中的敌人结算为一次攻击。
/// 命中窗口由攻击序列中的 OpenHitWindow / CloseHitWindow 事件控制，
/// 因此 bridge 是近战手感与命中时机的关键组件。
/// </summary>
[RequireComponent(typeof(WeaponSequenceBridge))]
public class MeleeWeapon : Weapon
{
    [Header("Inspector")]
    [Tooltip("近战命中盒的参考原点。通常是武器前端、棍头或刀刃附近。")]
    [SerializeField] private Transform hitDetectionTransform;
    private AttackSequenceDefinitionSO attackSequence;
    [Tooltip("必需组件：负责驱动攻击动作，并在关键帧时开关命中窗口。")]
    [SerializeField] private WeaponSequenceBridge sequenceBridge;

    private readonly Dictionary<int, HashSet<HealthComponent>> hitWindowTargets = new();
    private readonly Dictionary<int, MeleeHitDetectionPose> hitWindowLastPoses = new();
    private readonly HashSet<int> activeHitWindows = new();
    private MeleeWeaponAttackExecutor attackExecutor;
    private Entity pendingTarget;
    private AttackSequenceDefinitionSO runtimeDefaultSequence;

    private Vector2 HitBoxSize => WeaponData != null ? WeaponData.MeleeHitBoxSize : Vector2.one;

    protected override void Awake()
    {
        base.Awake();
        attackExecutor = new MeleeWeaponAttackExecutor(hitDetectionTransform);
        sequenceBridge = GetComponent<WeaponSequenceBridge>();
        sequenceBridge.SequenceEventTriggered += OnSequenceEventTriggered;
        sequenceBridge.SequenceCompleted += FinishAttackSequence;
    }

    protected override void OnConfiguredFromData()
    {
        if (attackSequence == null && WeaponData != null)
        {
            attackSequence = WeaponData.AttackSequence;
        }

        if (attackSequence == null)
        {
            runtimeDefaultSequence = WeaponAnimationSequencePresets.CreatePreset(WeaponAnimationSequencePresetId.MeleeHeavySwing);
            attackSequence = runtimeDefaultSequence;
        }

        ApplyHitDetectionOffset();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        ForceResetAttackState();
    }

    private void OnDestroy()
    {
        sequenceBridge.SequenceEventTriggered -= OnSequenceEventTriggered;
        sequenceBridge.SequenceCompleted -= FinishAttackSequence;
    }

    protected override bool CanStartAttack()
    {
        return !IsAttacking && !sequenceBridge.IsPlaying;
    }

    protected override AttackSequenceDefinitionSO GetEquippedAttackSequence()
    {
        return attackSequence;
    }

    protected override void TickWeapon(float deltaTime)
    {
        base.TickWeapon(deltaTime);

        if (activeHitWindows.Count == 0)
        {
            return;
        }

        MeleeHitDetectionPose currentPose = attackExecutor.CaptureCurrentPose();
        foreach (int windowId in activeHitWindows)
        {
            if (!hitWindowTargets.TryGetValue(windowId, out HashSet<HealthComponent> hitTargets))
            {
                continue;
            }

            if (!hitWindowLastPoses.TryGetValue(windowId, out MeleeHitDetectionPose previousPose))
            {
                previousPose = currentPose;
            }

            attackExecutor.ExecuteAttack(
                BuildAttackContext(pendingTarget, hitDetectionTransform),
                HitBoxSize,
                hitTargets,
                targetLayerMask,
                previousPose,
                currentPose);

            hitWindowLastPoses[windowId] = currentPose;
        }
    }

    protected override void BeginAttack(Entity target)
    {
        IsAttacking = true;
        pendingTarget = target;
        activeHitWindows.Clear();
        hitWindowTargets.Clear();
        hitWindowLastPoses.Clear();

        float sequenceDuration = ResolveAttackSequenceDuration(attackSequence);
        // 动态帧不是在播放过程中实时追目标，
        // 而是在这次攻击开始时，结合当前目标位置 + 当前 RuntimeStats.Range 先解出一份本轮专属轨迹。
        float reachScale = Mathf.Max(0.1f, RuntimeStats.Range);
        IReadOnlyDictionary<int, Vector3> dynamicPositionOverrides = BuildDynamicPositionOverrides(target);
        sequenceBridge.Play(attackSequence, dynamicPositionOverrides, sequenceDuration, reachScale);
    }

    public void OpenHitWindow(int windowId)
    {
        activeHitWindows.Add(windowId);
        if (!hitWindowTargets.TryGetValue(windowId, out HashSet<HealthComponent> hitTargets))
        {
            hitTargets = new HashSet<HealthComponent>();
            hitWindowTargets[windowId] = hitTargets;
        }
        else
        {
            hitTargets.Clear();
        }

        hitWindowLastPoses[windowId] = attackExecutor.CaptureCurrentPose();
    }

    public void CloseHitWindow(int windowId)
    {
        activeHitWindows.Remove(windowId);
        hitWindowLastPoses.Remove(windowId);
    }

    public void FinishAttackSequence()
    {
        activeHitWindows.Clear();
        hitWindowTargets.Clear();
        hitWindowLastPoses.Clear();
        pendingTarget = null;
        CompleteAttackCycle();
    }

    private IReadOnlyDictionary<int, Vector3> BuildDynamicPositionOverrides(Entity target)
    {
        if (attackSequence == null || target == null)
        {
            return null;
        }

        IReadOnlyList<WeaponMotionKeyframe> keyframes = attackSequence.MotionKeyframes;
        if (keyframes == null || keyframes.Count == 0)
        {
            return null;
        }

        Vector2 localTarget = transform.InverseTransformPoint(target.Center);
        float attackRange = Mathf.Max(0.1f, RuntimeStats.Range);
        Dictionary<int, Vector3> overrides = null;

        for (int i = 0; i < keyframes.Count; i++)
        {
            WeaponMotionKeyframe keyframe = keyframes[i];
            if (keyframe.xPositionMode != WeaponMotionPositionMode.DynamicFromTarget &&
                keyframe.yPositionMode != WeaponMotionPositionMode.DynamicFromTarget)
            {
                continue;
            }

            Vector3 resolvedPosition = ResolveDynamicKeyframePosition(keyframe, localTarget, attackRange);
            overrides ??= new Dictionary<int, Vector3>();
            overrides[i] = resolvedPosition;
        }

        return overrides;
    }

    private static Vector2 ResolveDynamicKeyframePosition(WeaponMotionKeyframe keyframe, Vector2 localTarget, float attackRange)
    {
        float normalizedTargetDistance = Mathf.Clamp01(localTarget.magnitude / attackRange);

        float resolvedX = keyframe.localPositionX;
        if (keyframe.xPositionMode == WeaponMotionPositionMode.DynamicFromTarget)
        {
            float minReach = Mathf.Clamp01(keyframe.xDynamicMinNormalizedReach);
            float maxReach = Mathf.Clamp(keyframe.xDynamicMaxNormalizedReach, minReach, 1f);
            float normalizedResolvedDistance = keyframe.dynamicPositionStrategy switch
            {
                WeaponMotionDynamicPositionStrategy.TowardTargetClampedRadius => Mathf.Clamp(normalizedTargetDistance, minReach, maxReach),
                _ => Mathf.Clamp01(Mathf.Abs(keyframe.localPositionX))
            };

            resolvedX = normalizedResolvedDistance * attackRange * Mathf.Sign(keyframe.localPositionX == 0f ? 1f : keyframe.localPositionX);
        }

        float resolvedY = keyframe.localPositionY;
        if (keyframe.yPositionMode == WeaponMotionPositionMode.DynamicFromTarget)
        {
            float minReach = Mathf.Clamp01(keyframe.yDynamicMinNormalizedReach);
            float maxReach = Mathf.Clamp(keyframe.yDynamicMaxNormalizedReach, minReach, 1f);
            float normalizedResolvedDistance = keyframe.dynamicPositionStrategy switch
            {
                WeaponMotionDynamicPositionStrategy.TowardTargetClampedRadius => Mathf.Clamp(normalizedTargetDistance, minReach, maxReach),
                _ => Mathf.Clamp01(Mathf.Abs(keyframe.localPositionY))
            };

            resolvedY = normalizedResolvedDistance * attackRange * Mathf.Sign(keyframe.localPositionY == 0f ? 1f : keyframe.localPositionY);
        }

        return new Vector2(resolvedX, resolvedY);
    }

    private void OnSequenceEventTriggered(WeaponSequenceEventContext eventContext)
    {
        switch (eventContext.EventType)
        {
            case WeaponSequenceEventType.OpenHitWindow:
                OpenHitWindow(eventContext.WindowId);
                break;
            case WeaponSequenceEventType.CloseHitWindow:
                CloseHitWindow(eventContext.WindowId);
                break;
            case WeaponSequenceEventType.PlaySfx:
                break;
            case WeaponSequenceEventType.PlayVfx:
                break;
        }
    }

    private void ForceResetAttackState()
    {
        activeHitWindows.Clear();
        hitWindowTargets.Clear();
        hitWindowLastPoses.Clear();
        pendingTarget = null;
        CompleteAttackCycle();
        sequenceBridge.Stop(true);
    }

    private void ApplyHitDetectionOffset()
    {
        if (hitDetectionTransform == null || WeaponData == null)
        {
            return;
        }

        Vector3 localPosition = hitDetectionTransform.localPosition;
        Vector2 hitOffset = WeaponData.MeleeHitOffset;
        localPosition.x = hitOffset.x;
        localPosition.y = hitOffset.y;
        hitDetectionTransform.localPosition = localPosition;
    }

    private void OnDrawGizmosSelected()
    {
        if (hitDetectionTransform == null)
        {
            return;
        }

        Gizmos.color = Color.red;
        Matrix4x4 previousMatrix = Gizmos.matrix;

        Vector3 previewPosition = hitDetectionTransform.position;
        Quaternion previewRotation = hitDetectionTransform.rotation;
        if (!Application.isPlaying && WeaponData != null)
        {
            Vector2 previewOffset = WeaponData.MeleeHitOffset;
            previewPosition = transform.TransformPoint(new Vector3(previewOffset.x, previewOffset.y, hitDetectionTransform.localPosition.z));
            previewRotation = transform.rotation * Quaternion.Euler(0f, 0f, hitDetectionTransform.localEulerAngles.z);
        }

        Gizmos.matrix = Matrix4x4.TRS(previewPosition, previewRotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, HitBoxSize);
        Gizmos.matrix = previousMatrix;
    }
}
