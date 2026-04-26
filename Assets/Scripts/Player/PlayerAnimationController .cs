using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(PlayerController))]
public class PlayerAnimationController : EntityComponentBase
{
    private static readonly int IS_MOVING_HASH = Animator.StringToHash("IsMoving");
    private static readonly int DIE_HASH = Animator.StringToHash("Die");

    [SerializeField] private Animator animator;
    [SerializeField] private EntityRenderer entityRenderer;

    private Player owner;
    private IMovable playerController;
    private HealthComponent healthComponent;
    private bool isDead;

    public override Entity Owner => owner;

    public Animator Animator => animator;

    public override void Initialize(Entity owner)
    {
        this.owner = owner as Player;
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

        animator.runtimeAnimatorController = this.owner.CharacterData.CharacterAnimatorController;
    }

    public override void OnEnableComponent()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDied += OnDied;
        }
    }

    public override void OnDisableComponent()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDied -= OnDied;
        }
    }

    public override void OnTick(float deltaTime)
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
