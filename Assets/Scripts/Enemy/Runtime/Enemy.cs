using System;
using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(EnemyCombatController))]
[RequireComponent(typeof(EnemyRuntimeBridge))]
[RequireComponent(typeof(CombatConfigBinder))]
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
    private CombatConfigBinder combatConfigBinder;

    private MoveBase activeMovement;
    private AttackBase activeAttack;
    private Entity targetEntity;
    private EnemyDefinitionSO runtimeDefinition;

    public override IMovement MoveComponent => activeMovement;
    public override Vector2 Center => transform.position + new Vector3(0f, collider != null ? collider.offset.y : 0f, 0f);
    public override EntityRenderer EntityRenderer => entityRenderer;

    public EnemyRole Role => role;
    public Entity TargetEntity => targetEntity;
    public float AttackDetectionRadius => attackDetectionRadius;

    private void Awake()
    {
        healthComponent = GetComponent<HealthComponent>();
        combatController = GetComponent<EnemyCombatController>();
        runtimeBridge = GetComponent<EnemyRuntimeBridge>();
        combatConfigBinder = GetComponent<CombatConfigBinder>();

        if (entityRenderer == null)
        {
            entityRenderer = GetComponentInChildren<EntityRenderer>();
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
        ApplyResolvedLoadout();
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
        ApplyResolvedLoadout();
        ConfigureCombat();
    }

    public void SetCombatLoadout(MoveBase movement, AttackBase attack)
    {
        activeMovement = movement;
        activeAttack = attack;

        if (combatController != null)
        {
            combatController.Initialize(this, activeMovement, activeAttack);
        }
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

        if (healthComponent != null)
        {
            healthComponent.Initialize(resolvedMaxHealth);
        }
    }

    private void ApplyResolvedLoadout()
    {
        if (runtimeDefinition == null || combatConfigBinder == null)
        {
            return;
        }

        combatConfigBinder.Apply(runtimeDefinition, this);
    }

    private void ConfigureCombat()
    {
        if (combatController == null || targetEntity == null || activeAttack == null)
        {
            return;
        }

        combatController.Configure(targetEntity, attackDetectionRadius);
    }
}
