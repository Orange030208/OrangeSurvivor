using System;
using UnityEngine;

public abstract class AttackBase : MonoBehaviour
{
    [SerializeField] private Transform attackOrigin;

    private Entity owner;
    private float attackTimer;
    private bool isInitialized;

    public event Action OnAttackExecuted;

    public void Initialize(Entity ownerEntity, Transform originTransform)
    {
        owner = ownerEntity ?? throw new ArgumentNullException(nameof(ownerEntity));
        attackOrigin = originTransform != null ? originTransform : transform;
        isInitialized = true;
        attackTimer = GetAttackInterval();
    }

    public bool Tick(Entity target, float attackRange, float deltaTime)
    {
        EnsureInitialized();

        if (!GameSimulation.IsRunning || target == null)
        {
            return false;
        }

        attackTimer += deltaTime;
        if (attackTimer < GetAttackInterval())
        {
            return false;
        }

        if (!IsTargetInRange(target, attackRange))
        {
            return false;
        }

        attackTimer = 0f;
        ExecuteAttack(target);
        OnAttackExecuted?.Invoke();
        return true;
    }

    public void LaunchProjectile(IProjectile projectile, in ProjectileLaunchContext context)
    {
        if (projectile == null)
        {
            throw new ArgumentNullException(nameof(projectile));
        }

        if (context.ProjectileDefinition != null)
        {
            AudioSfxBridge.RequestPlay(context.ProjectileDefinition.LaunchSfxKey);
        }

        projectile.Launch(context);
    }

    protected Entity Owner => owner;
    protected Transform AttackOrigin => attackOrigin != null ? attackOrigin : transform;

    protected bool IsTargetInRange(Entity target, float attackRange)
    {
        if (target == null)
        {
            return false;
        }

        return Vector2.Distance(target.transform.position, AttackOrigin.position) <= Mathf.Max(0f, attackRange);
    }

    protected void EnsureInitialized()
    {
        if (!isInitialized)
        {
            throw new InvalidOperationException($"{nameof(AttackBase)} must be initialized before use.");
        }
    }

    protected abstract float GetAttackInterval();
    protected abstract void ExecuteAttack(Entity target);
}
