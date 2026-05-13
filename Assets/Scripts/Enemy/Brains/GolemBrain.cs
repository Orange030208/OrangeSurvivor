using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyAttackController))]
public class GolemBrain : EnemyBrain
{
    private const string CHARGE_MODIFIER_SOURCE = "GolemBrain_Charge";
    private const string POST_CHARGE_ATTACK_MODIFIER_SOURCE = "GolemBrain_PostChargeAttack";
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

    [Header("攻击点位")]
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
            float damage = PropValueUtility.ClampNonNegative(
                propertiesManager.GetPropValue(PropType.Attack) * enemyData.ChargeDamageMultiplier);
            Vector2 knockbackDirection = hitEntity.Center - owner.Center;
            HitService.Apply(new HitRequest(
                owner,
                hitEntity,
                HitSpec.EnemyHitSpec(damage),
                hitEntity.Center,
                knockbackDirection,
                HitSourceKind.Direct,
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

    private sealed class AttackState : EnemyActionStateBase<GolemAIState>
    {
        private readonly GolemBrain brain;

        public AttackState(GolemBrain brain) : base(GolemAIState.Attack)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.currentMovable.StopMoving();

            if (brain.target == null)
            {
                brain.stateMachine.RequestState(GolemAIState.Idle, StateChangeMode.Force);
                return;
            }

            if (!brain.attackStrategy.CanUse(brain.target))
            {
                brain.stateMachine.RequestState(GolemAIState.Chase, StateChangeMode.Force);
                return;
            }

            brain.FaceTarget();
            BeginAction(brain.enemyData.AttackAction, brain.currentAnimatable);
        }

        public override void OnUpdate()
        {
            brain.FaceTarget();
            TickAction(Time.deltaTime);
        }

        public override void OnFixedUpdate()
        {
            brain.currentMovable.StopMoving();
        }

        protected override void OnActionCommit()
        {
            brain.attackStrategy.TryExecuteCommitted(brain.target);
        }

        protected override void OnActionComplete()
        {
            if (brain.target == null)
            {
                brain.stateMachine.RequestState(GolemAIState.Idle);
                return;
            }

            if (brain.ShouldEnterBerserk())
            {
                brain.stateMachine.RequestState(GolemAIState.BerserkWindup);
                return;
            }

            brain.stateMachine.RequestState(brain.attackStrategy.DetectionStrategy.IsTargetInRange(brain.target)
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

    private sealed class BerserkChargeState : EnemyActionStateBase<GolemAIState>
    {
        private readonly GolemBrain brain;

        public BerserkChargeState(GolemBrain brain) : base(GolemAIState.BerserkCharge)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.chargeHitTargets.Clear();
            brain.CaptureChargeDirection();
            brain.ApplyChargeModifiers();
            brain.currentAnimatable.SetPlaybackSpeed(brain.enemyData.ChargeAnimationSpeedMultiplier);
            BeginAction(brain.enemyData.ChargeAction, brain.currentAnimatable);
            brain.facingController?.FaceDirection(brain.chargeDirection);
            brain.DealChargeDamage();
        }

        public override void OnUpdate()
        {
            TickAction(Time.deltaTime);
        }

        public override void OnFixedUpdate()
        {
            brain.currentMovable.MoveTo(brain.owner.Center + brain.chargeDirection);
            brain.facingController?.FaceDirection(brain.chargeDirection);
            brain.DealChargeDamage();
        }

        public override void OnExit()
        {
            base.OnExit();
            brain.currentMovable.StopMoving();
            brain.currentAnimatable.ResetPlaybackSpeed();
            brain.RemoveChargeModifiers();
        }

        protected override void OnActionComplete()
        {
            brain.stateMachine.RequestState(GolemAIState.PostChargeAttack);
        }
    }

    private sealed class PostChargeAttackState : EnemyActionStateBase<GolemAIState>
    {
        private readonly GolemBrain brain;

        public PostChargeAttackState(GolemBrain brain) : base(GolemAIState.PostChargeAttack)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.currentMovable.StopMoving();
            brain.ApplyPostChargeAttackModifiers();
            brain.FaceTarget();
            BeginAction(brain.enemyData.PostChargeAttackAction, brain.currentAnimatable);
        }

        public override void OnUpdate()
        {
            brain.FaceTarget();
            TickAction(Time.deltaTime);
        }

        public override void OnFixedUpdate()
        {
            brain.currentMovable.StopMoving();
        }

        public override void OnExit()
        {
            base.OnExit();
            brain.RemovePostChargeAttackModifiers();
        }

        protected override void OnActionCommit()
        {
            TryCommitPostChargeAttack();
        }

        protected override void OnActionComplete()
        {
            brain.stateMachine.RequestState(GolemAIState.BerserkRecovery);
        }

        private void TryCommitPostChargeAttack()
        {
            Vector2 attackCenter = brain.ResolveMeleeAttackCenter();
            float attackRadius = PropValueUtility.DistancePointsToEffectiveAttackRangeWorldUnits(
                brain.propertiesManager.GetPropValue(PropType.AttackRange));
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
                float damage = PropValueUtility.ClampNonNegative(brain.propertiesManager.GetPropValue(PropType.Attack));
                HitService.Apply(new HitRequest(
                    brain.owner,
                    hitEntity,
                    HitSpec.EnemyHitSpec(damage),
                    hitPoint,
                    knockbackDirection,
                    HitSourceKind.Direct,
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

    private sealed class BerserkRecoveryState : EnemyActionStateBase<GolemAIState>
    {
        private readonly GolemBrain brain;

        public BerserkRecoveryState(GolemBrain brain) : base(GolemAIState.BerserkRecovery)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.ResetBerserkTimer();
            brain.currentMovable.StopMoving();
            brain.FaceTarget();
            BeginAction(brain.enemyData.RecoveryAction, brain.currentAnimatable);
        }

        public override void OnUpdate()
        {
            brain.FaceTarget();
            TickAction(Time.deltaTime);
        }

        public override void OnFixedUpdate()
        {
            brain.currentMovable.StopMoving();
        }

        protected override void OnActionComplete()
        {
            brain.stateMachine.RequestState(brain.target == null ? GolemAIState.Idle : GolemAIState.Chase);
        }
    }
}
