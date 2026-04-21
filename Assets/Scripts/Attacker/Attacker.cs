using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Attacker : MonoBehaviour, IProjectileLauncher
{
    [SerializeField] private Transform attackOrigin;

    private Entity owner;
    private bool isInitialized;

    public event Action OnAttackExecuted;

    public void Initialize(Entity ownerEntity, Transform originTransform)
    {
        owner = ownerEntity ?? throw new ArgumentNullException(nameof(ownerEntity), $"{nameof(Attacker)} requires {nameof(Entity)} owner.");
        attackOrigin = originTransform != null ? originTransform : transform;
        isInitialized = true;
    }

    public void EnsureInitialized()
    {
        if (!isInitialized)
        {
            throw new InvalidOperationException($"{nameof(Attacker)} must be initialized before use.");
        }
    }

    public Transform ResolveAttackOrigin()
    {
        return attackOrigin != null ? attackOrigin : transform;
    }

    public bool IsTargetInRange(Entity target, float attackDetectionRadius)
    {
        if (target == null)
        {
            return false;
        }

        Vector2 originPosition = ResolveAttackOrigin().position;
        return Vector2.Distance(target.transform.position, originPosition) <= Mathf.Max(0f, attackDetectionRadius);
    }

    public void ExecuteDirectAttack(Entity target, AttackDefinitionSO attackDefinition)
    {
        EnsureInitialized();

        if (target == null)
        {
            return;
        }

        if (attackDefinition == null)
        {
            throw new ArgumentNullException(nameof(attackDefinition));
        }

        HealthComponent healthComponent = target.GetComponent<HealthComponent>();
        if (healthComponent == null || healthComponent.OwnerEntity == null)
        {
            return;
        }

        HitSpec hitSpec = new HitSpec(attackDefinition.Damage, 0f, 1f);
        HitService.Apply(new HitRequest(
            owner,
            healthComponent.OwnerEntity,
            hitSpec,
            healthComponent.transform.position,
            HitSourceKind.Direct,
            owner != null ? owner.GetType().Name : nameof(Attacker)));

        OnAttackExecuted?.Invoke();
    }

    public void ExecuteProjectileAttack(
        Entity target,
        ProjectileAttackDefinitionSO attackDefinition,
        float angleOffset = 0f,
        int burstId = 0,
        EnemyProjectileAttackMode attackMode = EnemyProjectileAttackMode.Single,
        ProjectilePatternConfig patternConfig = default,
        bool useAbsoluteAngle = false)
    {
        EnsureInitialized();

        if (target == null)
        {
            return;
        }

        if (attackDefinition == null)
        {
            throw new ArgumentNullException(nameof(attackDefinition));
        }

        Vector2 originPosition = ResolveAttackOrigin().position;
        Vector2 aimDirection = useAbsoluteAngle
            ? (Vector2)(Quaternion.Euler(0f, 0f, angleOffset) * Vector2.right)
            : (Vector2)(Quaternion.Euler(0f, 0f, angleOffset) * (target.Center - originPosition).normalized);
        HitSpec hitSpec = new HitSpec(attackDefinition.Damage, 0f, 1f);

        Projectile projectile = ProjectileFactory.CreateProjectile(attackDefinition.ProjectileDefinition, originPosition, Quaternion.identity);
        LayerMask targetLayerMask = BuildTargetLayerMask(target);
        LaunchProjectile(projectile, new ProjectileLaunchContext(
            this,
            owner,
            originPosition,
            aimDirection,
            hitSpec,
            targetLayerMask,
            0,
            attackDefinition.ProjectileDefinition,
            burstId,
            ConvertAttackMode(attackMode),
            patternConfig.Equals(default(ProjectilePatternConfig)) ? attackDefinition.PatternConfig : patternConfig));

        OnAttackExecuted?.Invoke();
    }

    public void LaunchProjectile(IProjectile projectile, in ProjectileLaunchContext context)
    {
        if (projectile == null)
        {
            throw new ArgumentNullException(nameof(projectile), $"{nameof(Attacker)} requires a valid {nameof(IProjectile)} instance.");
        }

        if (context.ProjectileDefinition != null)
        {
            AudioSfxBridge.RequestPlay(context.ProjectileDefinition.LaunchSfxKey);
        }

        projectile.Launch(context);
    }

    private static ProjectileFiringMode ConvertAttackMode(EnemyProjectileAttackMode attackMode)
    {
        return attackMode switch
        {
            EnemyProjectileAttackMode.Spread => ProjectileFiringMode.Spread,
            EnemyProjectileAttackMode.Burst => ProjectileFiringMode.Burst,
            EnemyProjectileAttackMode.Nova => ProjectileFiringMode.Nova,
            _ => ProjectileFiringMode.Default
        };
    }

    private static LayerMask BuildTargetLayerMask(Entity targetEntity)
    {
        if (targetEntity == null)
        {
            throw new ArgumentNullException(nameof(targetEntity), $"{nameof(Attacker)} requires {nameof(Entity)} target to build target layer mask.");
        }

        return 1 << targetEntity.gameObject.layer;
    }
}
