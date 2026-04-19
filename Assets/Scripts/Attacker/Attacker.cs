using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Attacker : MonoBehaviour
{
    [SerializeField] private Transform attackOrigin;

    private Entity owner;
    private Entity target;
    private AttackDefinitionSO runtimeAttackDefinition;
    private AttackController attackController;
    private IAttackStateProvider[] attackStateProviders = Array.Empty<IAttackStateProvider>();
    private float attackDetectionRadius;
    private bool isInitialized;

    public bool HasAttackController => attackController != null;

    public void Initialize(Entity ownerEntity, Transform originTransform)
    {
        owner = ownerEntity ?? throw new ArgumentNullException(nameof(ownerEntity), $"{nameof(Attacker)} requires {nameof(Entity)} owner.");
        attackOrigin = originTransform != null ? originTransform : transform;
        attackStateProviders = ResolveAttackStateProviders();
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
        RebuildAttackController();
    }

    public bool Tick(float deltaTime)
    {
        if (attackController == null)
        {
            throw new InvalidOperationException($"{nameof(Attacker)} requires a built {nameof(AttackController)} before {nameof(Tick)}.");
        }

        return attackController.Tick(deltaTime);
    }

    public Transform ResolveAttackOrigin()
    {
        return attackOrigin != null ? attackOrigin : transform;
    }

    private void RebuildAttackController()
    {
        if (runtimeAttackDefinition == null)
        {
            throw new InvalidOperationException($"{nameof(Attacker)} requires {nameof(AttackDefinitionSO)} before rebuilding {nameof(AttackController)}.");
        }

        IAttackExecutor attackExecutor = AttackExecutorFactory.Create(new AttackExecutorBuildContext(owner, ResolveAttackOrigin(), runtimeAttackDefinition));
        attackController = new AttackController(
            runtimeAttackDefinition.AttackInterval,
            CanExecuteAttack,
            IsTargetInAttackRange,
            BuildAttackContext,
            attackExecutor);
    }

    private bool CanExecuteAttack()
    {
        if (attackStateProviders.Length == 0)
        {
            return true;
        }

        AttackStateContext context = new AttackStateContext(owner, target, ResolveAttackOrigin());
        for (int i = 0; i < attackStateProviders.Length; i++)
        {
            if (!attackStateProviders[i].CanAttack(context))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsTargetInAttackRange()
    {
        return target != null
            && Vector2.Distance(target.transform.position, transform.position) <= attackDetectionRadius;
    }

    private AttackContext BuildAttackContext()
    {
        Vector2 attackOriginPosition = ResolveAttackOrigin().position;
        Vector2 attackDirection = target != null
            ? (target.Center - attackOriginPosition).normalized
            : Vector2.zero;
        HitSpec hitSpec = new HitSpec(runtimeAttackDefinition.Damage, 0f, 1f);

        return new AttackContext(owner, target, attackOriginPosition, attackDirection, hitSpec);
    }

    private IAttackStateProvider[] ResolveAttackStateProviders()
    {
        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        List<IAttackStateProvider> providers = new List<IAttackStateProvider>();

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IAttackStateProvider provider)
            {
                providers.Add(provider);
            }
        }

        return providers.ToArray();
    }
}
