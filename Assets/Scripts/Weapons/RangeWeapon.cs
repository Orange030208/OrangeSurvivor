using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 远程武器：负责索敌、进入攻击、播放攻击序列，并在合适时机发射投射物。
/// 它不直接维护子弹行为，只负责把攻击意图转换成 projectile 发射。
/// 正式弹射物配置优先由序列事件直接引用 ProjectileDefinitionSO，
/// WeaponDataSO 仅通过 sequenceProjectileList 提供对应事件 key 的发射配置。
/// 
/// 运行时可变配置约定：
/// - 发射点、额外枪口、ProjectileDefinition 等可能在装配后才稳定；
/// - 因此投射物发射参数在开火时按当前配置直接解析，而不是提前缓存运行时执行器。
/// </summary>
[RequireComponent(typeof(WeaponSequenceBridge))]
public class RangeWeapon : Weapon, IProjectileLauncher
{
    [Header("Inspector")]
    [Tooltip("默认发射点。单枪口武器通常只需要配置这个点。")]
    [SerializeField] private Transform shootingPoint;
    [Tooltip("额外发射点。用于双枪、多炮口或多枪管武器。索引由攻击序列中的生成配置指定。")]
    [SerializeField] private Transform[] additionalShootingPoints;
    [Tooltip("攻击序列资源。为空时会在运行时生成默认远程序列。")]
    [SerializeField] private AttackSequenceDefinitionSO attackSequence;
    [Tooltip("必需组件：负责驱动武器动作序列并把关键帧事件转发回本类。")]
    [SerializeField] private WeaponSequenceBridge sequenceBridge;

    private Vector2 pendingTargetPosition;
    private int activeBurstId = -1;
    private AttackSequenceDefinitionSO runtimeDefaultSequence;

    public AttackSequenceDefinitionSO DebugAttackSequence => attackSequence != null ? attackSequence : runtimeDefaultSequence;

    protected override void Awake()
    {
        base.Awake();
        sequenceBridge = GetComponent<WeaponSequenceBridge>();
    }

    protected override void OnConfiguredFromData()
    {
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
        pendingTargetPosition = target.Center;
        LockAttackDirection(ResolveAttackDirection(pendingTargetPosition));
        activeBurstId = -1;
        float sequenceDuration = ResolveAttackSequenceDuration(attackSequence);
        float reachScale = Mathf.Max(0.1f, Range);
        sequenceBridge.Play(attackSequence, sequenceDuration, reachScale);
    }

    private void OnSequenceEventTriggered(WeaponSequenceEventType eventType, int eventKey)
    {
        switch (eventType)
        {
            case WeaponSequenceEventType.SpawnProjectile:
                FireProjectiles(eventKey);
                break;
            case WeaponSequenceEventType.PlaySfx:
                PlaySequenceSfx(eventKey);
                break;
            case WeaponSequenceEventType.PlayVfx:
                PlaySequenceVfx(eventKey);
                break;
        }
    }

    private void FireProjectiles(int eventKey)
    {
        if (WeaponData == null || !WeaponData.TryGetSequenceProjectile(eventKey, out WeaponSequenceProjectileDefinition projectileConfig))
        {
            return;
        }

        FireProjectiles(projectileConfig);
    }

    private void FireProjectiles(WeaponSequenceProjectileDefinition projectileConfig)
    {
        switch (projectileConfig.FiringMode)
        {
            case ProjectileFiringMode.Burst:
                TryStartBurst(projectileConfig);
                break;
            case ProjectileFiringMode.Spread:
                FireSpread(projectileConfig);
                break;
            case ProjectileFiringMode.Nova:
                FireNova(projectileConfig);
                break;
            default:
                FireSingle(projectileConfig, null);
                break;
        }
    }

    private void TryStartBurst(WeaponSequenceProjectileDefinition projectileConfig)
    {
        if (activeBurstId == projectileConfig.BurstId)
        {
            return;
        }

        activeBurstId = projectileConfig.BurstId;
        StartCoroutine(BurstRoutine(projectileConfig));
    }

    private IEnumerator BurstRoutine(WeaponSequenceProjectileDefinition projectileConfig)
    {
        int burstCount = projectileConfig.PatternConfig.BurstCount;
        float burstInterval = projectileConfig.PatternConfig.BurstInterval;

        for (int i = 0; i < burstCount; i++)
        {
            while (!GameSimulation.IsRunning)
            {
                yield return null;
            }

            FireSingle(projectileConfig, null);
            if (i < burstCount - 1)
            {
                yield return new WaitForSeconds(burstInterval);
            }
        }

        activeBurstId = -1;
    }

    private void FireSpread(WeaponSequenceProjectileDefinition projectileConfig)
    {
        int spreadCount = projectileConfig.PatternConfig.SpreadCount;
        if (spreadCount <= 1)
        {
            FireSingle(projectileConfig, null);
            return;
        }

        float spreadAngle = projectileConfig.PatternConfig.SpreadAngle;
        float step = spreadCount > 1 ? (spreadAngle * 2f) / (spreadCount - 1) : 0f;
        for (int i = 0; i < spreadCount; i++)
        {
            float angle = -spreadAngle + (step * i);
            FireSingle(projectileConfig, angle);
        }
    }

    private void FireNova(WeaponSequenceProjectileDefinition projectileConfig)
    {
        int novaCount = projectileConfig.PatternConfig.NovaCount;
        for (int i = 0; i < novaCount; i++)
        {
            float angle = 360f / novaCount * i;
            FireSingle(projectileConfig, angle);
        }
    }

    private void FireSingle(WeaponSequenceProjectileDefinition projectileConfig, float? angleOffset)
    {
        Transform origin = ResolveOrigin(projectileConfig.SpawnPointIndex);
        Entity sourceEntity = ResolveAttackSourceEntity();
        HitSpec hitSpec = BuildHitSpec();
        Vector2 aimDirection = ResolveAttackDirection(pendingTargetPosition, origin);
        if (angleOffset.HasValue)
        {
            aimDirection = (Quaternion.Euler(0f, 0f, angleOffset.Value) * aimDirection).normalized;
        }

        ExecuteProjectileAttack(sourceEntity, origin, aimDirection, hitSpec, projectileConfig);
    }

    private Transform ResolveOrigin(int spawnPointIndex)
    {
        if (additionalShootingPoints != null && spawnPointIndex >= 0 && spawnPointIndex < additionalShootingPoints.Length && additionalShootingPoints[spawnPointIndex] != null)
        {
            return additionalShootingPoints[spawnPointIndex];
        }

        return shootingPoint != null ? shootingPoint : transform;
    }

    public void LaunchProjectile(IProjectile projectile, in ProjectileLaunchContext context)
    {
        if (projectile == null)
        {
            throw new ArgumentNullException(nameof(projectile), $"{nameof(RangeWeapon)} requires a valid {nameof(IProjectile)} instance.");
        }

        if (context.ProjectileDefinition != null)
        {
            AudioSfxBridge.RequestPlay(context.ProjectileDefinition.LaunchSfxKey);
        }

        projectile.Launch(context);
    }

    private void ExecuteProjectileAttack(
        Entity sourceEntity,
        Transform origin,
        Vector2 aimDirection,
        HitSpec hitSpec,
        WeaponSequenceProjectileDefinition projectileConfig)
    {
        Projectile projectile = ProjectileFactory.CreateProjectile(projectileConfig.ProjectileDefinition, origin.position, Quaternion.identity);
        LaunchProjectile(projectile, new ProjectileLaunchContext(
            this,
            sourceEntity,
            origin.position,
            aimDirection,
            hitSpec,
            TargetLayerMask,
            projectileConfig.ProjectileDefinition,
            projectileConfig.SpawnPointIndex,
            projectileConfig.BurstId,
            projectileConfig.FiringMode,
            projectileConfig.PatternConfig));
    }

    private void PlaySequenceSfx(int eventKey)
    {
        if (WeaponData == null || !WeaponData.TryGetSequenceSfx(eventKey, out WeaponSequenceSfxDefinition definition))
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
        if (WeaponData == null || !WeaponData.TryGetSequenceVfx(eventKey, out WeaponSequenceVfxDefinition definition))
        {
            return;
        }

        if (definition.VfxPrefab != null)
        {
            Transform spawnAnchor = ResolveOrigin(definition.SpawnPointIndex);
            Vector3 spawnPosition = spawnAnchor.TransformPoint(definition.LocalOffset);
            Quaternion spawnRotation = spawnAnchor.rotation * Quaternion.Euler(definition.LocalEulerAngles);
            RuntimeVfx.Spawn(definition.VfxPrefab, spawnPosition, spawnRotation, null);
        }
    }

    private void FinishAttackSequence()
    {
        pendingTargetPosition = Vector2.zero;
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

        var events = sequence.EventKeyframes;
        for (int i = 0; i < events.Count; i++)
        {
            WeaponSequenceEventKeyframe keyframe = events[i];
            if (keyframe.eventType != WeaponSequenceEventType.SpawnProjectile)
            {
                continue;
            }

            Transform origin = null;
            if (WeaponData == null || !WeaponData.TryGetSequenceProjectile(keyframe.eventKey, out WeaponSequenceProjectileDefinition projectileConfig))
            {
                continue;
            }

            origin = ResolveOrigin(projectileConfig.SpawnPointIndex);
            if (origin == null)
            {
                continue;
            }

            DrawProjectilePatternGizmo(origin, projectileConfig);
        }
    }

    private void DrawProjectilePatternGizmo(Transform origin, WeaponSequenceProjectileDefinition projectileConfig)
    {
        Gizmos.color = projectileConfig.ProjectileDefinition != null ? projectileConfig.ProjectileDefinition.DebugColor : Color.cyan;

        switch (projectileConfig.FiringMode)
        {
            case ProjectileFiringMode.Spread:
                DrawSpreadPattern(origin, projectileConfig.PatternConfig.SpreadCount, projectileConfig.PatternConfig.SpreadAngle);
                break;
            case ProjectileFiringMode.Nova:
                DrawNovaPattern(origin, projectileConfig.PatternConfig.NovaCount);
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
