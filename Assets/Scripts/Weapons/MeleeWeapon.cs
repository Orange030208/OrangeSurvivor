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
    [Tooltip("命中检测使用的碰撞盒。建议只作为检测范围使用，不依赖物理碰撞回调。")]
    [SerializeField] private BoxCollider2D hitCollider;
    [Tooltip("攻击序列资源。为空时会在运行时生成默认的重棍挥击序列。")]
    [SerializeField] private AttackSequenceDefinitionSO attackSequence;
    [Tooltip("必需组件：负责驱动攻击动作，并在关键帧时开关命中窗口。")]
    [SerializeField] private WeaponSequenceBridge sequenceBridge;

    private readonly Dictionary<int, HashSet<HealthComponent>> hitWindowTargets = new();
    private readonly HashSet<int> activeHitWindows = new();
    private MeleeWeaponAttackExecutor attackExecutor;
    private Entity pendingTarget;
    private AttackSequenceDefinitionSO runtimeDefaultSequence;

    protected override void Awake()
    {
        base.Awake();
        attackExecutor = new MeleeWeaponAttackExecutor(hitDetectionTransform, hitCollider, targetLayerMask);

        sequenceBridge = GetComponent<WeaponSequenceBridge>();

        if (attackSequence == null && WeaponData != null)
        {
            attackSequence = WeaponData.AttackSequence;
        }

        if (attackSequence == null)
        {
            runtimeDefaultSequence = WeaponAnimationSequencePresets.CreatePreset(WeaponAnimationSequencePresetId.TitanMaulOverheadBreak);
            attackSequence = runtimeDefaultSequence;
        }

        sequenceBridge.SequenceEventTriggered += OnSequenceEventTriggered;
        sequenceBridge.SequenceCompleted += FinishAttackSequence;
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

        foreach (int windowId in activeHitWindows)
        {
            if (!hitWindowTargets.TryGetValue(windowId, out HashSet<HealthComponent> hitTargets))
            {
                continue;
            }

            attackExecutor.ExecuteAttack(BuildAttackContext(pendingTarget, hitDetectionTransform), hitTargets);
        }
    }

    protected override void BeginAttack(Entity target)
    {
        IsAttacking = true;
        pendingTarget = target;
        activeHitWindows.Clear();
        hitWindowTargets.Clear();

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
    }

    public void CloseHitWindow(int windowId)
    {
        activeHitWindows.Remove(windowId);
    }

    public void FinishAttackSequence()
    {
        activeHitWindows.Clear();
        hitWindowTargets.Clear();
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
            if (keyframe.positionMode != WeaponMotionPositionMode.DynamicFromTarget)
            {
                continue;
            }

            Vector3 resolvedPosition = ResolveDynamicKeyframePosition(keyframe, localTarget, attackRange);
            overrides ??= new Dictionary<int, Vector3>();
            overrides[i] = resolvedPosition;
        }

        return overrides;
    }

    private static Vector3 ResolveDynamicKeyframePosition(WeaponMotionKeyframe keyframe, Vector2 localTarget, float attackRange)
    {
        float targetDistance = localTarget.magnitude;
        Vector2 direction = targetDistance > 0.0001f ? localTarget.normalized : Vector2.right;

        float normalizedTargetDistance = Mathf.Clamp01(targetDistance / attackRange);
        float minReach = Mathf.Clamp01(keyframe.dynamicMinNormalizedReach);
        float maxReach = Mathf.Clamp(keyframe.dynamicMaxNormalizedReach, minReach, 1f);

        // 当前仅保留 TowardTargetClampedRadius：
        // - dynamicMinNormalizedReach / dynamicMaxNormalizedReach 表示 0~1 的“归一化攻击半径区间”；
        // - 1 表示恰好到达当前武器 RuntimeStats.Range 的边界；
        // - 运行时只在最后一步乘 attackRange，避免再把 Range 当倍率重复放大。
        float normalizedResolvedDistance = keyframe.dynamicPositionStrategy switch
        {
            WeaponMotionDynamicPositionStrategy.TowardTargetClampedRadius => Mathf.Clamp(normalizedTargetDistance, minReach, maxReach),
            _ => Mathf.Clamp01(keyframe.localPosition.magnitude)
        };

        float resolvedDistance = normalizedResolvedDistance * attackRange;
        return new Vector3(direction.x * resolvedDistance, direction.y * resolvedDistance, keyframe.localPosition.z);
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
        pendingTarget = null;
        CompleteAttackCycle();
        sequenceBridge.Stop(true);
    }

    private void OnDrawGizmosSelected()
    {
        DrawSharedWeaponDebugGizmos();

        if (hitDetectionTransform == null || hitCollider == null)
        {
            return;
        }

        Gizmos.color = Color.red;
        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(hitDetectionTransform.position, hitDetectionTransform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, hitCollider.size);
        Gizmos.matrix = previous;
    }
}
