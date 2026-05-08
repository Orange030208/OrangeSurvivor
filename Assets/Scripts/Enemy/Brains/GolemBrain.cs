using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyAttackController))]
public class GolemBrain : EnemyBrain
{
    private const string CHARGE_MODIFIER_SOURCE = "GolemBrain_Charge";
    private const string POST_CHARGE_ATTACK_MODIFIER_SOURCE = "GolemBrain_PostChargeAttack";
    private const string CHARGE_HIT_SOURCE_ID = "GolemBrain_ChargeHit";
    private const int CHARGE_HIT_BUFFER_SIZE = 32;

    public enum GolemAIState
    {
        Idle,
        Chase,
        Attack,
        BerserkWindup,
        BerserkCharge,
        PostChargeAttack,
        BerserkRecovery
    }

    private readonly StateMachine<GolemAIState> stateMachine = new();
    private readonly HashSet<Entity> chargeHitTargets = new();
    private readonly Collider2D[] chargeHitBuffer = new Collider2D[CHARGE_HIT_BUFFER_SIZE];

    [Header("Attack Points")]
    [SerializeField] private Transform meleePointTransform;

    private EnemyAttackController attackController;
    private GolemEnemySO enemyData;
    private IMoveStrategy chaseMoveStrategy;
    private IAttackStrategy attackStrategy;
    private float berserkTimer;
    private Vector2 chargeDirection = Vector2.right;
    private bool chargeModifiersApplied;
    private bool postChargeAttackModifiersApplied;
    private bool hasWarnedMissingMeleePoint;

    protected override void OnInitialize(Entity owner)
    {
        base.OnInitialize(owner);

        attackController = owner.GetComponent<EnemyAttackController>();
        enemyData = this.owner.EnemyData as GolemEnemySO;

        if (attackController == null)
        {
            throw new MissingComponentException($"{nameof(GolemBrain)} requires an {nameof(EnemyAttackController)}.");
        }

        if (enemyData == null)
        {
            throw new ArgumentException($"{nameof(GolemBrain)} requires a {nameof(GolemEnemySO)} definition.", nameof(owner));
        }
    }

    protected override void OnBrainStart()
    {
        BuildRuntimeStrategies();
        RegisterStates();
        ResetBerserkTimer();
        stateMachine.ChangeState(GolemAIState.Chase);
    }

    protected override void OnBrainUpdate()
    {
        TickBerserkTimer();
        stateMachine.Update();
    }

    protected override void OnBrainFixedUpdate()
    {
        stateMachine.FixedUpdate();
    }

    public override void StopBrain()
    {
        ResetChargePlaybackSpeed();
        RemoveChargeModifiers();
        RemovePostChargeAttackModifiers();
        base.StopBrain();
    }

    public override void StartBrain()
    {
        ResetChargePlaybackSpeed();
        RemoveChargeModifiers();
        RemovePostChargeAttackModifiers();
        ResetBerserkTimer();
        base.StartBrain();

        if (stateMachine.HasState)
        {
            stateMachine.ChangeState(GolemAIState.Chase, true);
        }
    }

    public override void OnDisableComponent()
    {
        ResetChargePlaybackSpeed();
        RemoveChargeModifiers();
        RemovePostChargeAttackModifiers();
    }

    private void RegisterStates()
    {
        stateMachine.RegisterState(new IdleState(this));
        stateMachine.RegisterState(new ChaseState(this));
        stateMachine.RegisterState(new AttackState(this));
        stateMachine.RegisterState(new BerserkWindupState(this));
        stateMachine.RegisterState(new BerserkChargeState(this));
        stateMachine.RegisterState(new PostChargeAttackState(this));
        stateMachine.RegisterState(new BerserkRecoveryState(this));
    }

    private void BuildRuntimeStrategies()
    {
        chaseMoveStrategy = new DirectChaseMoveStrategy(currentMovable);
        IRangeDetectionStrategy attackDetectionStrategy = new DistanceRangeDetectionStrategy(owner, propertiesManager);
        attackStrategy = new DirectDamageAttackStrategy(
            owner,
            attackController,
            propertiesManager,
            GolemEnemySO.ATTACK_ACTION_ID,
            enemyData.AttackSpeedBenefitRatio,
            attackDetectionStrategy,
            meleePointTransform);
    }

    private void TickBerserkTimer()
    {
        if (IsInBerserkSequence())
        {
            return;
        }

        berserkTimer -= Time.deltaTime;
    }

    private bool ShouldEnterBerserk()
    {
        return target != null && berserkTimer <= 0f && !IsInBerserkSequence();
    }

    private bool IsInBerserkSequence()
    {
        return stateMachine.IsCurrentState(GolemAIState.BerserkWindup) ||
               stateMachine.IsCurrentState(GolemAIState.BerserkCharge) ||
               stateMachine.IsCurrentState(GolemAIState.PostChargeAttack) ||
               stateMachine.IsCurrentState(GolemAIState.BerserkRecovery);
    }

    private void ResetBerserkTimer()
    {
        berserkTimer = enemyData != null ? enemyData.BerserkInterval : 0f;
    }

    private void ApplyChargeModifiers()
    {
        if (chargeModifiersApplied)
        {
            return;
        }

        propertiesManager.AddModifiers(CHARGE_MODIFIER_SOURCE, enemyData.ChargeModifiers);
        chargeModifiersApplied = true;
    }

    private void RemoveChargeModifiers()
    {
        if (!chargeModifiersApplied)
        {
            return;
        }

        propertiesManager.RemoveModifiers(CHARGE_MODIFIER_SOURCE);
        chargeModifiersApplied = false;
    }

    private void ApplyPostChargeAttackModifiers()
    {
        if (postChargeAttackModifiersApplied)
        {
            return;
        }

        propertiesManager.AddModifiers(POST_CHARGE_ATTACK_MODIFIER_SOURCE, enemyData.PostChargeAttackModifiers);
        postChargeAttackModifiersApplied = true;
    }

    private void RemovePostChargeAttackModifiers()
    {
        if (!postChargeAttackModifiersApplied)
        {
            return;
        }

        propertiesManager.RemoveModifiers(POST_CHARGE_ATTACK_MODIFIER_SOURCE);
        postChargeAttackModifiersApplied = false;
    }

    private void ResetChargePlaybackSpeed()
    {
        currentAnimatable?.ResetPlaybackSpeed();
    }

    private void CaptureChargeDirection()
    {
        Vector2 direction = target != null ? target.Center - owner.Center : currentMovable.MoveDirection;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            direction = owner.transform.localScale.x < 0f ? Vector2.left : Vector2.right;
        }

        chargeDirection = direction.normalized;
    }

    private Vector2 ResolveMeleeAttackCenter()
    {
        if (meleePointTransform != null)
        {
            return meleePointTransform.position;
        }

        if (!hasWarnedMissingMeleePoint)
        {
            hasWarnedMissingMeleePoint = true;
            Debug.LogWarning($"{nameof(GolemBrain)} on {owner.name} is missing melee point. Falling back to owner center.", owner);
        }

        return owner.Center;
    }

    private void DealChargeDamage()
    {
        if (enemyData.ChargeDamageRadius <= 0f)
        {
            return;
        }

        int hitCount = Physics2D.OverlapCircleNonAlloc(owner.Center, enemyData.ChargeDamageRadius, chargeHitBuffer, attackController.AttackLayer);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = chargeHitBuffer[i];
            if (hitCollider == null)
            {
                continue;
            }

            Entity hitEntity = hitCollider.GetComponent<Entity>();
            if (hitEntity == null)
            {
                hitEntity = hitCollider.GetComponentInParent<Entity>();
            }

            if (hitEntity == null || hitEntity == owner || chargeHitTargets.Contains(hitEntity))
            {
                continue;
            }

            chargeHitTargets.Add(hitEntity);
            float damage = Mathf.Max(0f, propertiesManager.GetPropValue(PropType.Attack) * enemyData.ChargeDamageMultiplier);
            Vector2 knockbackDirection = hitEntity.Center - owner.Center;
            HitService.Apply(new HitRequest(
                owner,
                hitEntity,
                HitSpec.EnemyHitSpec(damage),
                hitEntity.Center,
                knockbackDirection,
                HitSourceKind.Direct,
                CHARGE_HIT_SOURCE_ID,
                sourcePosition: owner.Center));
        }
    }

    private sealed class IdleState : StateBase<GolemAIState>
    {
        private readonly GolemBrain brain;

        public IdleState(GolemBrain brain) : base(GolemAIState.Idle)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.currentMovable.StopMoving();
            brain.currentAnimatable.PlayState(brain.enemyData.AnimConfig.IdleHash);
        }

        public override void OnUpdate()
        {
            brain.FaceTarget();

            if (brain.target == null)
            {
                return;
            }

            if (brain.ShouldEnterBerserk())
            {
                brain.stateMachine.ChangeState(GolemAIState.BerserkWindup);
                return;
            }

            bool isTargetInRange = brain.attackStrategy.DetectionStrategy.IsTargetInRange(brain.target);
            if (!isTargetInRange)
            {
                brain.stateMachine.ChangeState(GolemAIState.Chase);
                return;
            }

            if (brain.attackStrategy.CanUse(brain.target))
            {
                brain.stateMachine.ChangeState(GolemAIState.Attack);
            }
        }
    }

    private sealed class ChaseState : StateBase<GolemAIState>
    {
        private readonly GolemBrain brain;

        public ChaseState(GolemBrain brain) : base(GolemAIState.Chase)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.currentAnimatable.PlayState(brain.enemyData.AnimConfig.MoveHash);
        }

        public override void OnUpdate()
        {
            brain.FaceTarget();

            if (brain.target == null)
            {
                brain.stateMachine.ChangeState(GolemAIState.Idle);
                return;
            }

            if (brain.ShouldEnterBerserk())
            {
                brain.stateMachine.ChangeState(GolemAIState.BerserkWindup);
                return;
            }

            if (brain.attackStrategy.CanUse(brain.target))
            {
                brain.stateMachine.ChangeState(GolemAIState.Attack);
            }
        }

        public override void OnFixedUpdate()
        {
            if (brain.target == null)
            {
                return;
            }

            brain.chaseMoveStrategy.ExecuteMove(brain.target);
            brain.FaceTarget();
        }
    }

    private sealed class AttackState : StateBase<GolemAIState>
    {
        private readonly GolemBrain brain;
        private bool attackStarted;
        private bool attackCommitted;
        private int attackStateHash;

        public AttackState(GolemBrain brain) : base(GolemAIState.Attack)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            attackStarted = false;
            attackCommitted = false;
            attackStateHash = brain.enemyData.AnimConfig.AttackHash;
            brain.currentMovable.StopMoving();

            if (brain.target == null)
            {
                brain.stateMachine.ChangeState(GolemAIState.Idle);
                return;
            }

            if (!brain.attackStrategy.CanUse(brain.target))
            {
                brain.stateMachine.ChangeState(GolemAIState.Chase);
                return;
            }

            attackStarted = true;
            brain.FaceTarget();
            brain.currentAnimatable.PlayState(attackStateHash);
        }

        public override void OnUpdate()
        {
            if (!attackStarted)
            {
                return;
            }

            if (brain.target == null)
            {
                brain.stateMachine.ChangeState(GolemAIState.Idle);
                return;
            }

            if (!brain.currentAnimatable.IsCurrentState(attackStateHash))
            {
                return;
            }

            float normalizedTime = brain.currentAnimatable.GetCurrentStateNormalizedTime();
            if (!attackCommitted && normalizedTime >= brain.enemyData.AttackCommitNormalizedTime)
            {
                attackCommitted = true;

                brain.attackStrategy.TryExecuteCommitted(brain.target);
            }

            if (normalizedTime >= 1f)
            {
                ChangeToNextState();
            }
        }

        public override void OnFixedUpdate()
        {
            brain.currentMovable.StopMoving();
        }

        private void ChangeToNextState()
        {
            if (brain.target == null)
            {
                brain.stateMachine.ChangeState(GolemAIState.Idle);
                return;
            }

            if (brain.ShouldEnterBerserk())
            {
                brain.stateMachine.ChangeState(GolemAIState.BerserkWindup);
                return;
            }

            brain.stateMachine.ChangeState(brain.attackStrategy.DetectionStrategy.IsTargetInRange(brain.target)
                ? GolemAIState.Idle
                : GolemAIState.Chase);
        }
    }

    private sealed class BerserkWindupState : StateBase<GolemAIState>
    {
        private readonly GolemBrain brain;
        private float elapsedTime;

        public BerserkWindupState(GolemBrain brain) : base(GolemAIState.BerserkWindup)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            elapsedTime = 0f;
            brain.currentMovable.StopMoving();
            brain.FaceTarget();
            brain.currentAnimatable.PlayState(brain.enemyData.AnimConfig.IdleHash);
        }

        public override void OnUpdate()
        {
            brain.FaceTarget();
            elapsedTime += Time.deltaTime;

            if (elapsedTime >= brain.enemyData.PreChargeStunDuration)
            {
                brain.stateMachine.ChangeState(GolemAIState.BerserkCharge);
            }
        }

        public override void OnFixedUpdate()
        {
            brain.currentMovable.StopMoving();
        }
    }

    private sealed class BerserkChargeState : StateBase<GolemAIState>
    {
        private readonly GolemBrain brain;
        private float elapsedTime;

        public BerserkChargeState(GolemBrain brain) : base(GolemAIState.BerserkCharge)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            elapsedTime = 0f;
            brain.chargeHitTargets.Clear();
            brain.CaptureChargeDirection();
            brain.ApplyChargeModifiers();
            brain.currentAnimatable.SetPlaybackSpeed(brain.enemyData.ChargeAnimationSpeedMultiplier);
            brain.currentAnimatable.PlayState(brain.enemyData.AnimConfig.MoveHash);
            brain.facingController?.FaceDirection(brain.chargeDirection);
            brain.DealChargeDamage();
        }

        public override void OnUpdate()
        {
            elapsedTime += Time.deltaTime;
            if (elapsedTime >= brain.enemyData.ChargeDuration)
            {
                brain.stateMachine.ChangeState(GolemAIState.PostChargeAttack);
            }
        }

        public override void OnFixedUpdate()
        {
            brain.currentMovable.MoveTo(brain.owner.Center + brain.chargeDirection);
            brain.facingController?.FaceDirection(brain.chargeDirection);
            brain.DealChargeDamage();
        }

        public override void OnExit()
        {
            brain.currentMovable.StopMoving();
            brain.currentAnimatable.ResetPlaybackSpeed();
            brain.RemoveChargeModifiers();
        }
    }

    private sealed class PostChargeAttackState : StateBase<GolemAIState>
    {
        private readonly GolemBrain brain;
        private bool attackCommitted;
        private int attackStateHash;

        public PostChargeAttackState(GolemBrain brain) : base(GolemAIState.PostChargeAttack)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            attackCommitted = false;
            attackStateHash = brain.enemyData.AnimConfig.AttackHash;
            brain.currentMovable.StopMoving();
            brain.ApplyPostChargeAttackModifiers();
            brain.FaceTarget();
            brain.currentAnimatable.PlayState(attackStateHash);
        }

        public override void OnUpdate()
        {
            brain.FaceTarget();

            if (!brain.currentAnimatable.IsCurrentState(attackStateHash))
            {
                return;
            }

            float normalizedTime = brain.currentAnimatable.GetCurrentStateNormalizedTime();
            if (!attackCommitted && normalizedTime >= brain.enemyData.AttackCommitNormalizedTime)
            {
                attackCommitted = true;
                TryCommitPostChargeAttack();
            }

            if (normalizedTime >= 1f)
            {
                brain.stateMachine.ChangeState(GolemAIState.BerserkRecovery);
            }
        }

        public override void OnFixedUpdate()
        {
            brain.currentMovable.StopMoving();
        }

        public override void OnExit()
        {
            brain.RemovePostChargeAttackModifiers();
        }

        private void TryCommitPostChargeAttack()
        {
            Vector2 attackCenter = brain.ResolveMeleeAttackCenter();
            float attackRadius = brain.propertiesManager.GetPropValue(PropType.AttackRange);
            int hitCount = Physics2D.OverlapCircleNonAlloc(
                attackCenter,
                attackRadius,
                brain.chargeHitBuffer,
                brain.attackController.AttackLayer);

            for (int i = 0; i < hitCount; i++)
            {
                Entity hitEntity = ResolveEntity(brain.chargeHitBuffer[i]);
                if (hitEntity == null || hitEntity == brain.owner)
                {
                    continue;
                }

                Vector2 hitPoint = hitEntity.GetClosestPointTo(attackCenter);
                Vector2 knockbackDirection = hitEntity.Center - brain.owner.Center;
                float damage = Mathf.Max(0f, brain.propertiesManager.GetPropValue(PropType.Attack));
                HitService.Apply(new HitRequest(
                    brain.owner,
                    hitEntity,
                    HitSpec.EnemyHitSpec(damage),
                    hitPoint,
                    knockbackDirection,
                    HitSourceKind.Direct,
                    GolemEnemySO.POST_CHARGE_ATTACK_ACTION_ID,
                    brain.owner.Center));
            }
        }

        private static Entity ResolveEntity(Collider2D hitCollider)
        {
            if (hitCollider == null)
            {
                return null;
            }

            Entity entity = hitCollider.GetComponent<Entity>();
            return entity != null ? entity : hitCollider.GetComponentInParent<Entity>();
        }
    }

    private sealed class BerserkRecoveryState : StateBase<GolemAIState>
    {
        private readonly GolemBrain brain;
        private float elapsedTime;

        public BerserkRecoveryState(GolemBrain brain) : base(GolemAIState.BerserkRecovery)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            elapsedTime = 0f;
            brain.ResetBerserkTimer();
            brain.currentMovable.StopMoving();
            brain.FaceTarget();
            brain.currentAnimatable.PlayState(brain.enemyData.AnimConfig.IdleHash);
        }

        public override void OnUpdate()
        {
            brain.FaceTarget();
            elapsedTime += Time.deltaTime;

            if (elapsedTime < brain.enemyData.PostChargeStunDuration)
            {
                return;
            }

            brain.stateMachine.ChangeState(brain.target == null ? GolemAIState.Idle : GolemAIState.Chase);
        }

        public override void OnFixedUpdate()
        {
            brain.currentMovable.StopMoving();
        }
    }
}
