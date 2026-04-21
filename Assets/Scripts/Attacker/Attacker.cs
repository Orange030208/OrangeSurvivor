using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Attacker : MonoBehaviour, IProjectileLauncher
{
    [SerializeField] private Transform attackOrigin;

    private Entity owner;
    private Entity target;
    private AttackDefinitionSO runtimeAttackDefinition;
    private float attackDetectionRadius;
    private float attackTimer;
    private bool isInitialized;

    public bool HasConfiguredAttack => runtimeAttackDefinition != null;

    public void Initialize(Entity ownerEntity, Transform originTransform)
    {
        owner = ownerEntity ?? throw new ArgumentNullException(nameof(ownerEntity), $"{nameof(Attacker)} requires {nameof(Entity)} owner.");
        attackOrigin = originTransform != null ? originTransform : transform;
        isInitialized = true;
    }

    public void Configure(Entity targetEntity, AttackDefinitionSO attackDefinition, float detectionRadius)
    {
        if (!isInitialized)
        {
            throw new InvalidOperationException($"{nameof(Attacker)} must be initialized before {nameof(Configure)}.");
        }

        target = targetEntity;
        runtimeAttackDefinition = attackDefinition ?? throw new ArgumentNullException(nameof(attackDefinition), $"{nameof(Attacker)} requires {nameof(AttackDefinitionSO)}.");
        attackDetectionRadius = Mathf.Max(0f, detectionRadius);
        attackTimer = runtimeAttackDefinition.AttackInterval;
    }

    public bool Tick(float deltaTime)
    {
        if (runtimeAttackDefinition == null)
        {
            throw new InvalidOperationException($"{nameof(Attacker)} requires {nameof(AttackDefinitionSO)} before {nameof(Tick)}.");
        }

        if (!GameSimulation.IsRunning)
        {
            return false;
        }

        attackTimer += deltaTime;
        if (attackTimer < runtimeAttackDefinition.AttackInterval)
        {
            return false;
        }

        if (!IsTargetInAttackRange())
        {
            return false;
        }

        attackTimer = 0f;
        ExecuteAttack();
        return true;
    }

    public Transform ResolveAttackOrigin()
    {
        return attackOrigin != null ? attackOrigin : transform;
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

    private bool IsTargetInAttackRange()
    {
        Vector2 originPosition = ResolveAttackOrigin().position;
        return target != null
            && Vector2.Distance(target.transform.position, originPosition) <= attackDetectionRadius;
    }

    private void ExecuteAttack()
    {
        if (target == null)
        {
            return;
        }

        Vector2 originPosition = ResolveAttackOrigin().position;
        Vector2 aimDirection = (target.Center - originPosition).normalized;
        HitSpec hitSpec = new HitSpec(runtimeAttackDefinition.Damage, 0f, 1f);

        switch (runtimeAttackDefinition.Type)
        {
            case AttackType.Direct:
                ExecuteDirectAttack(hitSpec);
                break;
            case AttackType.Projectile:
                ExecuteProjectileAttack(originPosition, aimDirection, hitSpec);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(runtimeAttackDefinition), runtimeAttackDefinition.Type, "Unsupported attack type.");
        }
    }

    private void ExecuteDirectAttack(HitSpec hitSpec)
    {
        if (target == null)
        {
            return;
        }

        HealthComponent healthComponent = target.GetComponent<HealthComponent>();
        if (healthComponent == null || healthComponent.OwnerEntity == null)
        {
            return;
        }

        HitService.Apply(new HitRequest(
            owner,
            healthComponent.OwnerEntity,
            hitSpec,
            healthComponent.transform.position,
            HitSourceKind.Direct,
            owner != null ? owner.GetType().Name : nameof(Attacker)));
    }

    private void ExecuteProjectileAttack(Vector2 originPosition, Vector2 aimDirection, HitSpec hitSpec)
    {
        if (runtimeAttackDefinition is not ProjectileAttackDefinitionSO projectileAttackDefinition)
        {
            throw new InvalidOperationException($"{nameof(Attacker)} requires {nameof(ProjectileAttackDefinitionSO)} for projectile attacks.");
        }

        Projectile projectile = ProjectileFactory.CreateProjectile(projectileAttackDefinition.ProjectileDefinition, originPosition, Quaternion.identity);
        LayerMask targetLayerMask = BuildTargetLayerMask(target);
        LaunchProjectile(projectile, new ProjectileLaunchContext(
            this,
            owner,
            originPosition,
            aimDirection,
            hitSpec,
            targetLayerMask,
            0,
            projectileAttackDefinition.ProjectileDefinition,
            0,
            ProjectileFiringMode.Default));
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
