using System;
using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(Movement))]
[RequireComponent(typeof(Attacker))]
[RequireComponent(typeof(EnemyCombatController))]
[RequireComponent(typeof(EnemyRuntimeBridge))]
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
    private Movement movement;
    private Attacker attacker;
    private EnemyCombatController combatController;
    private EnemyRuntimeBridge runtimeBridge;

    private Entity targetEntity;
    private EnemyDefinitionSO runtimeDefinition;
    private AttackDefinitionSO runtimeAttackDefinition;
    private EnemyMovementDefinitionSO runtimeMovementDefinition;

    public override IMovement MoveComponent => movement;
    public override Vector2 Center => transform.position + new Vector3(0f, collider != null ? collider.offset.y : 0f, 0f);
    public override EntityRenderer EntityRenderer => entityRenderer;

    public EnemyRole Role => role;
    public Entity TargetEntity => targetEntity;
    public AttackDefinitionSO RuntimeAttackDefinition => runtimeAttackDefinition;
    public EnemyMovementDefinitionSO RuntimeMovementDefinition => runtimeMovementDefinition;
    public float AttackDetectionRadius => attackDetectionRadius;

    private void Awake()
    {
        healthComponent = GetComponent<HealthComponent>();
        movement = GetComponent<Movement>();
        attacker = GetComponent<Attacker>();
        combatController = GetComponent<EnemyCombatController>();
        runtimeBridge = GetComponent<EnemyRuntimeBridge>();

        if (entityRenderer == null)
        {
            entityRenderer = GetComponentInChildren<EntityRenderer>();
        }

        if (combatController != null)
        {
            combatController.Initialize(this, attacker);
        }

        if (runtimeBridge != null)
        {
            runtimeBridge.Initialize(this, healthComponent);
        }
    }

    private void Start()
    {
        if (runtimeDefinition == null)
        {
            throw new InvalidOperationException($"{nameof(Enemy)} must be configured by factory or spawner before runtime. Missing {nameof(EnemyDefinitionSO)}.");
        }

        if (targetEntity == null)
        {
            throw new InvalidOperationException($"{nameof(Enemy)} must receive an explicit target {nameof(Entity)} from factory or spawner before runtime.");
        }

        ApplyResolvedStats();
        ConfigureCombat();
    }

    public void Configure(EnemyDefinitionSO definition, Entity target)
    {
        if (definition == null)
        {
            throw new ArgumentNullException(nameof(definition), $"{nameof(Enemy)} requires a non-null {nameof(EnemyDefinitionSO)}.");
        }

        if (target == null)
        {
            throw new ArgumentNullException(nameof(target), $"{nameof(Enemy)} requires an explicit non-null {nameof(Entity)} target.");
        }

        runtimeDefinition = definition;
        targetEntity = target;
        ApplyResolvedStats();
        ConfigureCombat();
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
        attackDetectionRadius = runtimeDefinition != null ? runtimeDefinition.AttackDetectionRadius : attackDetectionRadius;
        runtimeMovementDefinition = runtimeDefinition != null ? runtimeDefinition.MovementDefinition : null;
        runtimeAttackDefinition = runtimeDefinition != null ? runtimeDefinition.AttackDefinition : null;

        if (healthComponent != null)
        {
            healthComponent.Initialize(resolvedMaxHealth);
        }

        if (movement != null)
        {
            float resolvedMoveSpeed = runtimeDefinition != null ? runtimeDefinition.MoveSpeed : movement.MoveSpeed;
            movement.SetMoveSpeed(resolvedMoveSpeed);
        }
    }

    private void ConfigureCombat()
    {
        if (combatController == null || targetEntity == null)
        {
            return;
        }

        combatController.Configure(targetEntity, runtimeMovementDefinition, runtimeAttackDefinition, attackDetectionRadius);
    }
}
