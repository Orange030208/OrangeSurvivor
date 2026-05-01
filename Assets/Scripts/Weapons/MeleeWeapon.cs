using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

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
    private Vector2 pendingTargetPosition;
    private AttackSequenceDefinitionSO runtimeDefaultSequence;

    private Vector2 HitBoxSize => WeaponData.MeleeHitBoxSize;

    public AttackSequenceDefinitionSO DebugAttackSequence => attackSequence != null ? attackSequence : runtimeDefaultSequence;

    public override void Initialize(Entity owner)
    {
        base.Initialize(owner);
        attackExecutor = new MeleeWeaponAttackExecutor(hitDetectionTransform, SpawnMeleeHitVfx);
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
            runtimeDefaultSequence = WeaponAnimationSequencePresets.CreatePreset(WeaponAnimationSequencePresetId.MeleeHeavyHorizontalSlash);
            attackSequence = runtimeDefaultSequence;
        }

        ApplyHitDetectionOffset();
    }

    public override void OnDisableComponent()
    {
        base.OnDisableComponent();
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
                this,
                ResolveAttackSourceEntity(),
                BuildHitSpec(),
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
        pendingTargetPosition = target.Center;
        LockAttackDirection(ResolveAttackDirection(pendingTargetPosition));
        activeHitWindows.Clear();
        hitWindowTargets.Clear();
        hitWindowLastPoses.Clear();

        float sequenceDuration = ResolveAttackSequenceDuration(attackSequence);
        // 每次攻击开始时锁定目标相对坐标，序列播放器会按参考坐标和缩放权重重定向采样点。
        Vector2 targetLocalOffset = transform.InverseTransformPoint(target.Center);
        sequenceBridge.Play(attackSequence, targetLocalOffset, sequenceDuration);
    }

    public void OpenHitWindow(int eventKey)
    {
        activeHitWindows.Add(eventKey);
        if (!hitWindowTargets.TryGetValue(eventKey, out HashSet<HealthComponent> hitTargets))
        {
            hitTargets = new HashSet<HealthComponent>();
            hitWindowTargets[eventKey] = hitTargets;
        }
        else
        {
            hitTargets.Clear();
        }

        hitWindowLastPoses[eventKey] = attackExecutor.CaptureCurrentPose();
    }

    public void CloseHitWindow(int eventKey)
    {
        activeHitWindows.Remove(eventKey);
        hitWindowLastPoses.Remove(eventKey);
    }

    public void FinishAttackSequence()
    {
        activeHitWindows.Clear();
        hitWindowTargets.Clear();
        hitWindowLastPoses.Clear();
        pendingTargetPosition = Vector2.zero;
        CompleteAttackCycle();
    }

    private void OnSequenceEventTriggered(WeaponSequenceEventType eventType, int eventKey)
    {
        switch (eventType)
        {
            case WeaponSequenceEventType.OpenHitWindow:
                OpenHitWindow(eventKey);
                break;
            case WeaponSequenceEventType.CloseHitWindow:
                CloseHitWindow(eventKey);
                break;
            case WeaponSequenceEventType.PlaySfx:
                PlaySequenceSfx(eventKey);
                break;
            case WeaponSequenceEventType.PlayVfx:
                PlaySequenceVfx(eventKey);
                break;
        }
    }

    private void ForceResetAttackState()
    {
        activeHitWindows.Clear();
        hitWindowTargets.Clear();
        hitWindowLastPoses.Clear();
        pendingTargetPosition = Vector2.zero;
        CompleteAttackCycle();
        sequenceBridge.Stop(true);
    }

    private void ApplyHitDetectionOffset()
    {
        if (hitDetectionTransform == null)
        {
            return;
        }

        Vector3 localPosition = hitDetectionTransform.localPosition;
        Vector2 hitOffset = WeaponData.MeleeHitOffset;
        localPosition.x = hitOffset.x;
        localPosition.y = hitOffset.y;
        hitDetectionTransform.localPosition = localPosition;
    }

    private void PlaySequenceSfx(int windowId)
    {
        if (WeaponData == null || !WeaponData.TryGetSequenceSfx(windowId, out WeaponSequenceSfxDefinition definition))
        {
            return;
        }

        if (definition.SfxKey != AudioSfxKey.None)
        {
            AudioSfxBridge.RequestPlay(definition.SfxKey);
        }
    }

    private void PlaySequenceVfx(int windowId)
    {
        if (WeaponData == null || !WeaponData.TryGetSequenceVfx(windowId, out WeaponSequenceVfxDefinition definition))
        {
            return;
        }

        if (definition.VfxPrefab != null)
        {
            WeaponSpawnPointPose spawnAnchor = ResolveVfxSpawnPointPose(definition.SpawnPointIndex);
            Vector3 spawnPosition = spawnAnchor.Position + spawnAnchor.Rotation * definition.LocalOffset;
            Quaternion spawnRotation = spawnAnchor.Rotation * Quaternion.Euler(definition.LocalEulerAngles);
            RuntimeVfx.Spawn(definition.VfxPrefab, spawnPosition, spawnRotation, null);
        }
    }

    private WeaponSpawnPointPose ResolveVfxSpawnPointPose(int spawnPointIndex)
    {
        if (WeaponData != null && WeaponData.TryGetSpawnPointPose(spawnPointIndex, transform, out WeaponSpawnPointPose configuredPose))
        {
            return configuredPose;
        }

        Transform legacyAnchor = hitDetectionTransform != null ? hitDetectionTransform : transform;
        return new WeaponSpawnPointPose(legacyAnchor.position, legacyAnchor.rotation);
    }

    private void SpawnMeleeHitVfx(Vector2 hitPoint)
    {
        if (WeaponData == null || WeaponData.MeleeHitVfxPrefab == null)
        {
            return;
        }

        Quaternion spawnRotation = hitDetectionTransform != null ? hitDetectionTransform.rotation : transform.rotation;
        RuntimeVfx.Spawn(WeaponData.MeleeHitVfxPrefab, hitPoint, spawnRotation, null);
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

        if (WeaponData == null || WeaponData.SpawnPoints == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        for (int i = 0; i < WeaponData.SpawnPoints.Count; i++)
        {
            if (!WeaponData.TryGetSpawnPointPose(i, transform, out WeaponSpawnPointPose pose))
            {
                continue;
            }

            Gizmos.DrawWireSphere(pose.Position, 0.06f);
            Gizmos.DrawRay(pose.Position, pose.Forward * 0.6f);
        }
    }
}

internal readonly struct MeleeHitDetectionPose
{
    public Vector2 Position { get; }
    public float RotationZ { get; }

    public MeleeHitDetectionPose(Vector2 position, float rotationZ)
    {
        Position = position;
        RotationZ = rotationZ;
    }
}

internal sealed class MeleeWeaponAttackExecutor
{
    private readonly Transform hitOrigin;
    private readonly float innerCompensationRadius;
    private readonly System.Action<Vector2> hitVfxCallback;

    public MeleeWeaponAttackExecutor(Transform hitOrigin, System.Action<Vector2> hitVfxCallback, float innerCompensationRadius = 1.1f)
    {
        this.hitOrigin = hitOrigin;
        this.hitVfxCallback = hitVfxCallback;
        this.innerCompensationRadius = Mathf.Max(0.05f, innerCompensationRadius);
    }

    public void ExecuteAttack(Weapon weapon, Entity sourceEntity, HitSpec hitSpec, Vector2 hitBoxSize, HashSet<HealthComponent> hitTargets,
        LayerMask targetLayerMask, in MeleeHitDetectionPose fromPose, in MeleeHitDetectionPose toPose)
    {
        if (hitOrigin == null || hitTargets == null)
        {
            return;
        }

        int sampleCount = CalculateSampleCount(hitBoxSize, fromPose, toPose);
        for (int i = 0; i < sampleCount; i++)
        {
            float t = sampleCount == 1 ? 1f : i / (sampleCount - 1f);
            Vector2 sampledPosition = Vector2.Lerp(fromPose.Position, toPose.Position, t);
            float sampledAngle = Mathf.LerpAngle(fromPose.RotationZ, toPose.RotationZ, t);
            Collider2D[] colliders = Physics2D.OverlapBoxAll(sampledPosition, hitBoxSize, sampledAngle, targetLayerMask);
            ApplyDamage(colliders, weapon, sourceEntity, hitSpec, hitTargets, hitVfxCallback);
        }
    }

    public MeleeHitDetectionPose CaptureCurrentPose()
    {
        return hitOrigin == null
            ? default
            : new MeleeHitDetectionPose(hitOrigin.position, hitOrigin.eulerAngles.z);
    }

    private int CalculateSampleCount(Vector2 hitBoxSize, in MeleeHitDetectionPose fromPose, in MeleeHitDetectionPose toPose)
    {
        float positionDelta = Vector2.Distance(fromPose.Position, toPose.Position);
        float rotationDelta = Mathf.Abs(Mathf.DeltaAngle(fromPose.RotationZ, toPose.RotationZ));
        float minHitExtent = Mathf.Max(0.05f, Mathf.Min(hitBoxSize.x, hitBoxSize.y) * 0.5f);
        float positionStep = Mathf.Max(0.05f, minHitExtent / innerCompensationRadius);
        int positionSamples = Mathf.Max(1, Mathf.CeilToInt(positionDelta / positionStep) + 1);
        int rotationSamples = Mathf.Max(1, Mathf.CeilToInt(rotationDelta / 12f) + 1);
        return Mathf.Max(positionSamples, rotationSamples);
    }

    private static void ApplyDamage(Collider2D[] colliders, Weapon weapon, Entity sourceEntity, HitSpec hitSpec, HashSet<HealthComponent> hitTargets, System.Action<Vector2> hitVfxCallback)
    {
        for (int i = 0; i < colliders.Length; i++)
        {
            if (!colliders[i].TryGetComponent(out HealthComponent healthComponent))
            {
                continue;
            }

            if (hitTargets.Contains(healthComponent))
            {
                continue;
            }

            Entity target = healthComponent.GetComponent<Entity>();
            if (target == null)
            {
                continue;
            }

            hitTargets.Add(healthComponent);
            Vector2 knockbackDirection = sourceEntity != null
                ? target.Center - sourceEntity.Center
                : target.Center - (Vector2)healthComponent.transform.position;
            HitRequest request = new HitRequest(
                sourceEntity,
                target,
                hitSpec,
                healthComponent.transform.position,
                knockbackDirection,
                HitSourceKind.Weapon,
                weapon.GetType().Name);
            HitResult hitResult = weapon.ApplyHit(request);
            if (!hitResult.IsCancelled && !hitResult.IsDodged && !hitResult.IsBlocked && hitResult.FinalDamage > 0f)
            {
                hitVfxCallback?.Invoke(hitResult.HitPoint);
            }
        }
    }
}
