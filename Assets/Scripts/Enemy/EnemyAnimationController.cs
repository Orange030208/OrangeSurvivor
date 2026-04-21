using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(HealthComponent))]
public sealed class EnemyAnimationController : MonoBehaviour
{
    private static readonly int IS_MOVING_HASH = Animator.StringToHash("IsMoving");
    private static readonly int ATTACK_HASH = Animator.StringToHash("Attack");
    private static readonly int DIE_HASH = Animator.StringToHash("Die");

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private EntityRenderer entityRenderer;

    [Header("Animation")]
    [SerializeField] private bool useAttackTrigger = true;

    private Movement movement;
    private Attacker attacker;
    private HealthComponent healthComponent;
    private bool isDead;

    private void Awake()
    {
        movement = GetComponent<Movement>();
        attacker = GetComponent<Attacker>();
        healthComponent = GetComponent<HealthComponent>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (entityRenderer == null)
        {
            entityRenderer = GetComponentInChildren<EntityRenderer>();
        }
    }

    private void OnEnable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDied += OnDied;
        }

        if (attacker != null)
        {
            attacker.OnAttackExecuted += OnAttackExecuted;
        }
    }

    private void OnDisable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDied -= OnDied;
        }

        if (attacker != null)
        {
            attacker.OnAttackExecuted -= OnAttackExecuted;
        }
    }

    private void Update()
    {
        if (animator == null || isDead)
        {
            return;
        }

        bool isMoving = movement != null && movement.IsMoving;
        animator.SetBool(IS_MOVING_HASH, isMoving);
        UpdateFacing();
    }

    private void OnAttackExecuted()
    {
        if (animator == null || isDead || !useAttackTrigger)
        {
            return;
        }

        animator.SetTrigger(ATTACK_HASH);
    }

    private void OnDied()
    {
        if (animator == null || isDead)
        {
            return;
        }

        isDead = true;
        animator.SetBool(IS_MOVING_HASH, false);
        animator.SetTrigger(DIE_HASH);
    }

    private void UpdateFacing()
    {
        if (movement == null || entityRenderer == null || entityRenderer.SpriteRenderer == null)
        {
            return;
        }

        Vector2 moveDirection = movement.MoveDirection;
        if (Mathf.Abs(moveDirection.x) <= 0.001f)
        {
            return;
        }

        entityRenderer.SpriteRenderer.flipX = moveDirection.x < 0f;
    }
}
