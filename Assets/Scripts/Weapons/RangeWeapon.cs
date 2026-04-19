using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 远程武器：负责索敌、进入攻击、播放攻击序列，并在合适时机发射投射物。
/// 它不直接维护子弹行为，只负责把攻击意图转换成 projectile 发射。
/// 正式弹射物配置优先由序列事件直接引用 ProjectileDefinitionSO，
/// WeaponDataSO.ProjectileDefinitions 则作为武器级共享清单与回退来源。
/// 
/// 运行时可变配置约定：
/// - 发射点、额外枪口、ProjectileDefinition 等可能在装配后才稳定；
/// - 因此不要在 Awake 缓存 ProjectileWeaponAttackExecutor，发射时再按当前配置解析。
/// </summary>
[RequireComponent(typeof(WeaponSequenceBridge))]
public class RangeWeapon : Weapon
{
    [Header("Inspector")]
    [Tooltip("默认发射点。单枪口武器通常只需要配置这个点。")]
    [SerializeField] private Transform shootingPoint;
    [Tooltip("额外发射点。用于双枪、多炮口或多枪管武器。索引由 ProjectileSpawnPayload.SpawnPointIndex 指定。")]
    [SerializeField] private Transform[] additionalShootingPoints;
    [Tooltip("攻击序列资源。为空时会在运行时生成默认远程序列。")]
    [SerializeField] private AttackSequenceDefinitionSO attackSequence;
    [Tooltip("必需组件：负责驱动武器动作序列并把关键帧事件转发回本类。")]
    [SerializeField] private WeaponSequenceBridge sequenceBridge;

    private Entity pendingTarget;
    private int activeBurstId = -1;
    private AttackSequenceDefinitionSO runtimeDefaultSequence;
    private WeaponSequenceResourceResolver sequenceResourceResolver;

    public AttackSequenceDefinitionSO DebugAttackSequence => attackSequence != null ? attackSequence : runtimeDefaultSequence;

    protected override void Awake()
    {
        base.Awake();
        sequenceBridge = GetComponent<WeaponSequenceBridge>();
    }

    protected override void OnConfiguredFromData()
    {
        sequenceResourceResolver = new WeaponSequenceResourceResolver(WeaponData);

        if (attackSequence == null && WeaponData != null)
        {
            attackSequence = WeaponData.AttackSequence;
        }

        if (attackSequence == null)
        {
            runtimeDefaultSequence = WeaponAnimationSequencePresets.CreatePreset(WeaponAnimationSequencePresetId.RangedRifleKick);
            attackSequence = runtimeDefaultSequence;
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        sequenceBridge.SequenceEventTriggered -= OnSequenceEventTriggered;
        sequenceBridge.SequenceCompleted -= FinishAttackSequence;
        sequenceBridge.SequenceEventTriggered += OnSequenceEventTriggered;
        sequenceBridge.SequenceCompleted += FinishAttackSequence;
    }

    protected override void OnDisable()
    {
        sequenceBridge.SequenceEventTriggered -= OnSequenceEventTriggered;
        sequenceBridge.SequenceCompleted -= FinishAttackSequence;

        base.OnDisable();
        FinishAttackSequence();
        sequenceBridge.Stop(true);
        StopAllCoroutines();
    }

    protected override bool CanStartAttack()
    {
        return !IsAttacking && !sequenceBridge.IsPlaying;
    }

    protected override void BeginAttack(Entity target)
    {
        IsAttacking = true;
        pendingTarget = target;
        LockAttackDirection(ResolveAttackDirection(target));
        activeBurstId = -1;
        float sequenceDuration = ResolveAttackSequenceDuration(attackSequence);
        float reachScale = Mathf.Max(0.1f, RuntimeStats.Range);
        sequenceBridge.Play(attackSequence, sequenceDuration, reachScale);
    }

    private void OnSequenceEventTriggered(WeaponSequenceEventContext eventContext)
    {
        switch (eventContext.EventType)
        {
            case WeaponSequenceEventType.SpawnProjectile:
                FireProjectiles(eventContext.EventKey);
                break;
            case WeaponSequenceEventType.PlaySfx:
                PlaySequenceSfx(eventContext.EventKey);
                break;
            case WeaponSequenceEventType.PlayVfx:
                PlaySequenceVfx(eventContext.EventKey);
                break;
        }
    }

    private void FireProjectiles(int eventKey)
    {
        if (sequenceResourceResolver == null || !sequenceResourceResolver.TryGetProjectile(eventKey, out ProjectileSpawnPayload payload))
        {
            return;
        }

        FireProjectiles(payload);
    }

    private void FireProjectiles(ProjectileSpawnPayload payload)
    {
        if (pendingTarget == null)
        {
            return;
        }

        switch (payload.FiringMode)
        {
            case ProjectileFiringMode.Burst:
                TryStartBurst(payload);
                break;
            case ProjectileFiringMode.Spread:
                FireSpread(payload);
                break;
            case ProjectileFiringMode.Nova:
                FireNova(payload);
                break;
            default:
                FireSingle(payload, null);
                break;
        }
    }

    private void TryStartBurst(ProjectileSpawnPayload payload)
    {
        if (activeBurstId == payload.BurstId)
        {
            return;
        }

        activeBurstId = payload.BurstId;
        StartCoroutine(BurstRoutine(payload));
    }

    private IEnumerator BurstRoutine(ProjectileSpawnPayload payload)
    {
        int burstCount = payload.PatternConfig.BurstCount;
        float burstInterval = payload.PatternConfig.BurstInterval;

        for (int i = 0; i < burstCount; i++)
        {
            if (pendingTarget == null)
            {
                break;
            }

            while (!GameSimulation.IsRunning)
            {
                yield return null;
            }

            FireSingle(payload, null);
            if (i < burstCount - 1)
            {
                yield return new WaitForSeconds(burstInterval);
            }
        }

        activeBurstId = -1;
    }

    private void FireSpread(ProjectileSpawnPayload payload)
    {
        int spreadCount = payload.PatternConfig.SpreadCount;
        if (spreadCount <= 1)
        {
            FireSingle(payload, null);
            return;
        }

        float spreadAngle = payload.PatternConfig.SpreadAngle;
        float step = spreadCount > 1 ? (spreadAngle * 2f) / (spreadCount - 1) : 0f;
        for (int i = 0; i < spreadCount; i++)
        {
            float angle = -spreadAngle + (step * i);
            FireSingle(payload, angle);
        }
    }

    private void FireNova(ProjectileSpawnPayload payload)
    {
        int novaCount = payload.PatternConfig.NovaCount;
        for (int i = 0; i < novaCount; i++)
        {
            float angle = 360f / novaCount * i;
            FireSingle(payload, angle);
        }
    }

    private void FireSingle(ProjectileSpawnPayload payload, float? angleOffset)
    {
        ProjectileWeaponAttackExecutor executor = BuildExecutor(payload.ProjectileDefinition);
        Transform origin = ResolveOrigin(payload.SpawnPointIndex);
        WeaponAttackContext context = BuildAttackContext(pendingTarget, origin);
        if (angleOffset.HasValue)
        {
            Vector2 rotatedDirection = Quaternion.Euler(0f, 0f, angleOffset.Value) * context.AimDirection;
            context = new WeaponAttackContext(context.Weapon, context.SourceEntity, context.Origin, context.Target, rotatedDirection.normalized, context.Stats, context.HitSpec);
        }

        executor.ExecuteAttack(context, payload);
    }

    private ProjectileWeaponAttackExecutor BuildExecutor(ProjectileDefinitionSO projectileDefinition)
    {
        Projectile projectilePrefab = ResolveProjectilePrefab(projectileDefinition);
        return new ProjectileWeaponAttackExecutor(projectilePrefab, shootingPoint, additionalShootingPoints);
    }

    private Projectile ResolveProjectilePrefab(ProjectileDefinitionSO projectileDefinition)
    {
        if (projectileDefinition == null)
        {
            throw new ArgumentNullException(nameof(projectileDefinition), $"{nameof(RangeWeapon)} requires {nameof(ProjectileDefinitionSO)} for projectile attacks.");
        }

        return ResourcesManager.GetProjectilePrefab(projectileDefinition.TemplateKind);
    }

    private Transform ResolveOrigin(int spawnPointIndex)
    {
        if (additionalShootingPoints != null && spawnPointIndex >= 0 && spawnPointIndex < additionalShootingPoints.Length && additionalShootingPoints[spawnPointIndex] != null)
        {
            return additionalShootingPoints[spawnPointIndex];
        }

        return shootingPoint != null ? shootingPoint : transform;
    }

    private void PlaySequenceSfx(int eventKey)
    {
        if (sequenceResourceResolver == null || !sequenceResourceResolver.TryGetSfx(eventKey, out WeaponSequenceSfxDefinition definition))
        {
            return;
        }

        if (definition.SfxKey != AudioSfxKey.None)
        {
            AudioSfxBridge.RequestPlay(definition.SfxKey);
        }
    }

    private void PlaySequenceVfx(int eventKey)
    {
        if (sequenceResourceResolver == null || !sequenceResourceResolver.TryGetVfx(eventKey, out WeaponSequenceVfxDefinition definition))
        {
            return;
        }

        if (definition.VfxPrefab != null)
        {
            Transform spawnAnchor = ResolveOrigin(definition.SpawnPointIndex);
            Vector3 spawnPosition = spawnAnchor.TransformPoint(definition.LocalOffset);
            Quaternion spawnRotation = spawnAnchor.rotation * Quaternion.Euler(definition.LocalEulerAngles);
            GameObject instance =  Instantiate(definition.VfxPrefab, spawnPosition, spawnRotation);
            Destroy(instance, definition.VfxLifetime);
        }
    }

    private void FinishAttackSequence()
    {
        pendingTarget = null;
        activeBurstId = -1;
        CompleteAttackCycle();
    }

    private void OnDrawGizmosSelected()
    {
        if (shootingPoint == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(shootingPoint.position, 0.06f);
        Gizmos.DrawRay(shootingPoint.position, shootingPoint.up * 0.8f);

        if (additionalShootingPoints != null)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 1f);
            for (int i = 0; i < additionalShootingPoints.Length; i++)
            {
                Transform point = additionalShootingPoints[i];
                if (point == null)
                {
                    continue;
                }

                Gizmos.DrawWireSphere(point.position, 0.05f);
                Gizmos.DrawRay(point.position, point.up * 0.6f);
            }
        }

        AttackSequenceDefinitionSO sequence = DebugAttackSequence;
        if (sequence == null)
        {
            return;
        }

        WeaponSequenceResourceResolver previewResolver = new WeaponSequenceResourceResolver(WeaponData);
        var events = sequence.EventKeyframes;
        for (int i = 0; i < events.Count; i++)
        {
            WeaponSequenceEventKeyframe keyframe = events[i];
            if (keyframe.eventType != WeaponSequenceEventType.SpawnProjectile)
            {
                continue;
            }

            Transform origin = null;
            if (!previewResolver.TryGetProjectile(keyframe.eventKey, out ProjectileSpawnPayload payload))
            {
                continue;
            }

            origin = ResolveOrigin(payload.SpawnPointIndex);
            if (origin == null)
            {
                continue;
            }

            DrawProjectilePatternGizmo(origin, payload);
        }
    }

    private void DrawProjectilePatternGizmo(Transform origin, ProjectileSpawnPayload payload)
    {
        Gizmos.color = payload.ProjectileDefinition != null ? payload.ProjectileDefinition.DebugColor : Color.cyan;

        switch (payload.FiringMode)
        {
            case ProjectileFiringMode.Spread:
                DrawSpreadPattern(origin, payload.PatternConfig.SpreadCount, payload.PatternConfig.SpreadAngle);
                break;
            case ProjectileFiringMode.Nova:
                DrawNovaPattern(origin, payload.PatternConfig.NovaCount);
                break;
            default:
                Gizmos.DrawRay(origin.position, origin.up * 1.1f);
                break;
        }
    }

    private void DrawSpreadPattern(Transform origin, int count, float halfAngle)
    {
        if (count <= 1)
        {
            Gizmos.DrawRay(origin.position, origin.up * 1.1f);
            return;
        }

        float step = (halfAngle * 2f) / (count - 1);
        for (int i = 0; i < count; i++)
        {
            float angle = -halfAngle + step * i;
            Vector3 direction = Quaternion.Euler(0f, 0f, angle) * origin.up;
            Gizmos.DrawRay(origin.position, direction * 1.1f);
        }
    }

    private void DrawNovaPattern(Transform origin, int count)
    {
        count = Mathf.Max(1, count);
        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i;
            Vector3 direction = Quaternion.Euler(0f, 0f, angle) * Vector3.up;
            Gizmos.DrawRay(origin.position, direction * 1f);
        }
    }
}
