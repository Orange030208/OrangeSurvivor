using UnityEngine;

[RequireComponent(typeof(Player))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(ProceduralEntityAnimationComponent))]
[RequireComponent(typeof(HealthComponent))]
public sealed class PlayerProceduralAnimationDriver : EntityComponentBase
{
    private Player owner;
    private IMovable moveComponent;
    private HealthComponent healthComponent;
    private ProceduralEntityAnimationComponent animationComponent;
    private int lastStateHash;
    private bool isDead;

    public override Entity Owner => owner;

    // 先于 ProceduralEntityAnimationComponent 采样移动状态，避免状态切换晚一帧。
    public override int Priority => PriorityPreset.NoRely - 20;

    public override void Initialize(Entity owner)
    {
        this.owner = owner as Player;
        moveComponent = GetComponent<PlayerController>();
        healthComponent = GetComponent<HealthComponent>();
        animationComponent = GetComponent<ProceduralEntityAnimationComponent>();
        lastStateHash = 0;
        isDead = false;
    }

    public override void OnEnableComponent()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDeathSequenceStarted += OnDeathSequenceStarted;
        }
    }

    public override void OnDisableComponent()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDeathSequenceStarted -= OnDeathSequenceStarted;
        }
    }

    public override void OnTick(float deltaTime)
    {
        if (isDead ||
            owner == null ||
            moveComponent == null ||
            animationComponent == null ||
            owner.AnimationConfig == null ||
            owner.ProceduralAnimationProfile == null)
        {
            return;
        }

        int targetStateHash = moveComponent.IsMoving
            ? owner.AnimationConfig.MoveHash
            : owner.AnimationConfig.IdleHash;

        if (targetStateHash != 0 && targetStateHash != lastStateHash)
        {
            lastStateHash = targetStateHash;
            animationComponent.PlayState(targetStateHash, 0f);
        }

        animationComponent.FaceMoveDirection(moveComponent);
    }

    private void OnDeathSequenceStarted()
    {
        isDead = true;
        lastStateHash = 0;
    }
}
