using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(PlayerController))]
public class PlayerAnimationController : MonoBehaviour
{
    private static readonly int IS_MOVING_HASH = Animator.StringToHash("IsMoving");
    private static readonly int DIE_HASH = Animator.StringToHash("Die");

    [SerializeField] private Animator animator;
    [SerializeField] private EntityRenderer entityRenderer;

    private IMovable playerController;
    private HealthComponent healthComponent;
    private bool isDead;

    public Animator Animator => animator;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
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
    }

    private void OnDisable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDied -= OnDied;
        }
    }

    private void Update()
    {
        if (isDead) return;
        
        animator.SetBool(IS_MOVING_HASH, playerController.IsMoving);
        UpdateFacing();
    }

    private void OnDied()
    {
        if (isDead) return;

        isDead = true;
        animator.SetBool(IS_MOVING_HASH, false);
        animator.SetTrigger(DIE_HASH);
    }

    private void UpdateFacing()
    {
        Vector2 moveDirection = playerController.MoveDirection;
        if (Mathf.Abs(moveDirection.x) <= 0.001f)
        {
            return;
        }

        entityRenderer.SpriteRenderer.flipX = moveDirection.x < 0f;
    }
}