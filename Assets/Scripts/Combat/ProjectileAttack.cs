using UnityEngine;

public sealed class ProjectileAttack : AttackBase, IProjectileLauncher
{
    private float damage = 1f;
    private float attackFrequency = 1f;
    private ProjectileDefinitionSO projectileDefinition;
    private ProjectileFiringMode attackMode = ProjectileFiringMode.Default;
    private ProjectilePatternConfig patternConfig = default;

    public void SetDamage(float value)
    {
        damage = Mathf.Max(0f, value);
    }

    public void SetAttackFrequency(float value)
    {
        attackFrequency = Mathf.Max(0.01f, value);
    }

    public void SetProjectileDefinition(ProjectileDefinitionSO definition)
    {
        projectileDefinition = definition;
    }

    public void SetFiringMode(ProjectileFiringMode mode)
    {
        attackMode = mode;
    }

    public void SetPatternConfig(ProjectilePatternConfig config)
    {
        patternConfig = config;
    }

    protected override float GetAttackInterval()
    {
        return 1f / Mathf.Max(0.01f, attackFrequency);
    }

    protected override void ExecuteAttack(Entity target)
    {
        if (target == null || projectileDefinition == null)
        {
            return;
        }

        switch (attackMode)
        {
            case ProjectileFiringMode.Spread:
                ExecuteSpread(target);
                break;
            case ProjectileFiringMode.Burst:
                ExecuteBurst(target);
                break;
            case ProjectileFiringMode.Nova:
                ExecuteNova(target);
                break;
            default:
                ExecuteProjectile(target, 0f, 0, ProjectileFiringMode.Default, false);
                break;
        }
    }

    private ProjectilePatternConfig EffectivePatternConfig => patternConfig.Equals(default(ProjectilePatternConfig))
        ? ProjectilePatternConfig.Default
        : patternConfig;

    private void ExecuteSpread(Entity target)
    {
        int spreadCount = EffectivePatternConfig.SpreadCount;
        float spreadAngle = EffectivePatternConfig.SpreadAngle;
        float step = spreadCount > 1 ? (spreadAngle * 2f) / (spreadCount - 1) : 0f;
        for (int i = 0; i < spreadCount; i++)
        {
            float angle = -spreadAngle + (step * i);
            ExecuteProjectile(target, angle, 0, ProjectileFiringMode.Spread, false);
        }
    }

    private void ExecuteBurst(Entity target)
    {
        int burstCount = EffectivePatternConfig.BurstCount;
        for (int i = 0; i < burstCount; i++)
        {
            ExecuteProjectile(target, 0f, i, ProjectileFiringMode.Burst, false);
        }
    }

    private void ExecuteNova(Entity target)
    {
        int novaCount = EffectivePatternConfig.NovaCount;
        for (int i = 0; i < novaCount; i++)
        {
            float angle = 360f / novaCount * i;
            ExecuteProjectile(target, angle, 0, ProjectileFiringMode.Nova, true);
        }
    }

    private void ExecuteProjectile(Entity target, float angleOffset, int burstId, ProjectileFiringMode firingMode, bool useAbsoluteAngle)
    {
        Vector2 originPosition = AttackOrigin.position;
        Vector2 baseDirection = (target.Center - originPosition).sqrMagnitude > Mathf.Epsilon
            ? (target.Center - originPosition).normalized
            : Vector2.right;
        Vector2 aimDirection = useAbsoluteAngle
            ? (Vector2)(Quaternion.Euler(0f, 0f, angleOffset) * Vector2.right)
            : (Vector2)(Quaternion.Euler(0f, 0f, angleOffset) * baseDirection);
        HitSpec hitSpec = new HitSpec(Mathf.Max(0f, damage), 0f, 1f);

        Projectile projectile = ProjectileFactory.CreateProjectile(projectileDefinition, originPosition, Quaternion.identity);
        LayerMask targetLayerMask = BuildTargetLayerMask(target);
        LaunchProjectile(projectile, new ProjectileLaunchContext(
            this,
            Owner,
            originPosition,
            aimDirection,
            hitSpec,
            targetLayerMask,
            0,
            projectileDefinition,
            burstId,
            firingMode,
            EffectivePatternConfig));
    }

    private static LayerMask BuildTargetLayerMask(Entity targetEntity)
    {
        return 1 << targetEntity.gameObject.layer;
    }
}
