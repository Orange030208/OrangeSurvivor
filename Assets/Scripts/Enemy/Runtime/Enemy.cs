using System;
using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(EnemyCombatController))]
[RequireComponent(typeof(EnemyRuntimeBridge))]
[RequireComponent(typeof(EnemyBehaviorController))]
[RequireComponent(typeof(EnemyComponentRegistry))]
public class Enemy : Entity
{
    [Header("组件")]
    [SerializeField] private new Collider2D collider;
    [SerializeField] private EntityRenderer entityRenderer;

    [Header("默认配置")]
    [SerializeField] private float maxHealth = 1f;
    [SerializeField] private EnemyRole role = EnemyRole.Normal;
    [SerializeField] private float attackDetectionRadius = 1f;

    private HealthComponent healthComponent;
    private EnemyCombatController combatController;
    private EnemyRuntimeBridge runtimeBridge;
    private EnemyBehaviorController behaviorController;

    private MoveBase activeMovement;
    private AttackBase activeAttack;
    private Entity targetEntity;
    private EnemySO runtimeDefinition;

    public override IMovement MoveComponent => activeMovement;
    public override Vector2 Center => transform.position + new Vector3(0f, collider != null ? collider.offset.y : 0f, 0f);
    public override EntityRenderer EntityRenderer => entityRenderer;

    public EnemyRole Role => role;
    public Entity TargetEntity => targetEntity;
    public float AttackDetectionRadius => attackDetectionRadius;
    public EnemySO RuntimeDefinition => runtimeDefinition;
    public EnemyCombatController CombatController => combatController;
    public EnemyBehaviorController BehaviorController => behaviorController;

    private void Awake()
    {
        healthComponent = GetComponent<HealthComponent>();
        combatController = GetComponent<EnemyCombatController>();
        runtimeBridge = GetComponent<EnemyRuntimeBridge>();
        behaviorController = GetComponent<EnemyBehaviorController>();

        if (entityRenderer == null)
        {
            entityRenderer = GetComponentInChildren<EntityRenderer>();
        }

        runtimeBridge?.Initialize(this, healthComponent);
    }

    private void Start()
    {
        if (runtimeDefinition == null)
        {
            throw new InvalidOperationException($"{nameof(Enemy)} must be configured by factory or spawner before runtime. Missing {nameof(EnemySO)}.");
        }

        if (targetEntity == null)
        {
            throw new InvalidOperationException($"{nameof(Enemy)} must receive an explicit target {nameof(Entity)} from factory or spawner before runtime.");
        }

        ApplyResolvedStats();
        ConfigureBehavior();
        ConfigureCombat();
    }

    public void Configure(EnemySO definition, Entity target)
    {
        if (definition == null)
        {
            throw new ArgumentNullException(nameof(definition), $"{nameof(Enemy)} requires a non-null {nameof(EnemySO)}.");
        }

        if (target == null)
        {
            throw new ArgumentNullException(nameof(target), $"{nameof(Enemy)} requires an explicit non-null {nameof(Entity)} target.");
        }

        runtimeDefinition = definition;
        targetEntity = target;
        ApplyResolvedStats();
        ConfigureBehavior();
        ConfigureCombat();
    }

    public void SetCombatLoadout(MoveBase movement, AttackBase attack)
    {
        activeMovement = movement;
        activeAttack = attack;

        combatController?.Initialize(this, activeMovement, activeAttack);
    }

    public void SetActiveMovement(MoveBase movement)
    {
        activeMovement = movement;
        combatController?.SetActiveMovement(movement);
    }

    public void SetActiveAttack(AttackBase attack)
    {
        activeAttack = attack;
        combatController?.SetActiveAttack(attack);
    }

    public void SetAttackRange(float value)
    {
        attackDetectionRadius = Mathf.Max(0f, value);
        if (combatController != null && targetEntity != null)
        {
            combatController.Configure(targetEntity, attackDetectionRadius);
        }
    }

    public void SetAttackEnabled(bool enabled)
    {
        combatController?.SetAttackEnabled(enabled);
    }

    public void SetAllowMoveWhileAttacking(bool enabled)
    {
        combatController?.SetAllowMoveWhileAttacking(enabled);
    }

    public void PassAway()
    {
        runtimeBridge?.PassAway();
    }

    public void PassAwayAfterWave()
    {
        runtimeBridge?.PassAwayAfterWave();
    }

    private void ApplyResolvedStats()
    {
        float resolvedMaxHealth = runtimeDefinition != null ? runtimeDefinition.MaxHealth : maxHealth;
        role = runtimeDefinition != null ? runtimeDefinition.Role : role;
        attackDetectionRadius = runtimeDefinition != null ? runtimeDefinition.BaseDetectionRadius : attackDetectionRadius;

        if (healthComponent != null)
        {
            healthComponent.Initialize(resolvedMaxHealth);
        }
    }

    private void ConfigureBehavior()
    {
        if (runtimeDefinition == null || behaviorController == null)
        {
            return;
        }

        behaviorController.Configure(runtimeDefinition);
    }

    private void ConfigureCombat()
    {
        if (combatController == null || targetEntity == null)
        {
            return;
        }

        combatController.Initialize(this, activeMovement, activeAttack);
        combatController.Configure(targetEntity, attackDetectionRadius);
    }
}
