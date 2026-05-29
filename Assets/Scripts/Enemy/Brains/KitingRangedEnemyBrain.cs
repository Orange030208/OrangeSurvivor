using System;
using UnityEngine;

[RequireComponent(typeof(EnemyAttackController))]
public class KitingRangedEnemyBrain : EnemyBrain
{
    private enum KitingRangedAIState
    {
        Idle,
        Approach,
        Retreat,
        Attack
    }

    private readonly StateMachine<KitingRangedAIState> stateMachine = new();

    [Header("攻击点位")]
    [SerializeField] private Transform attackPointTransform;

    private EnemyAttackController attackController;
    private WormEnemySO enemyData;
    private IMoveStrategy approachMoveStrategy;
    private IMoveStrategy retreatMoveStrategy;
    private IAttackStrategy attackStrategy;

    protected override void OnInitialize(Entity owner)
    {
        base.OnInitialize(owner);

        attackController = owner.GetComponent<EnemyAttackController>();
        enemyData = this.owner.EnemyData as WormEnemySO;

        if (attackController == null)
        {
            throw new MissingComponentException($"{nameof(KitingRangedEnemyBrain)} requires an {nameof(EnemyAttackController)}.");
        }

        if (enemyData == null)
        {
            throw new ArgumentException($"{nameof(KitingRangedEnemyBrain)} requires a {nameof(WormEnemySO)} definition.", nameof(owner));
        }
    }

    protected override void OnBrainStart()
    {
        BuildRuntimeStrategies();
        RegisterStates();
        stateMachine.ChangeState(KitingRangedAIState.Approach);
    }

    protected override void OnBrainUpdate()
    {
        stateMachine.Update();
    }

    protected override void OnBrainFixedUpdate()
    {
        stateMachine.FixedUpdate();
    }

    public override void StartBrain()
    {
        bool shouldResetExistingState = HasBrainStarted;
        base.StartBrain();

        if (shouldResetExistingState && stateMachine.HasState)
        {
            stateMachine.ChangeState(KitingRangedAIState.Approach, true);
        }
    }

    private void RegisterStates()
    {
        stateMachine.RegisterState(new IdleState(this));
        stateMachine.RegisterState(new ApproachState(this));
        stateMachine.RegisterState(new RetreatState(this));
        stateMachine.RegisterState(new AttackState(this));
    }

    private void BuildRuntimeStrategies()
    {
        approachMoveStrategy = new DirectChaseMoveStrategy(currentMovable);
        retreatMoveStrategy = new RetreatMoveStrategy(owner, currentMovable, propertiesManager, enemyData.retreatMovement);

        attackStrategy = new ProjectileAttackStrategy(
            owner,
            attackController,
            propertiesManager,
            enemyData.AttackAction.ActionId,
            enemyData.attackSpeedBenefitRatio,
            attackPointTransform,
            enemyData.attackProjectileDefinition);
    }

    private bool ShouldRetreat()
    {
        return target != null && GetDistanceToTarget() < GetRetreatTriggerDistance();
    }

    private bool HasReachedRetreatSafeDistance()
    {
        return target == null || GetDistanceToTarget() >= GetRetreatCompleteDistance();
    }

    private float GetRetreatTriggerDistance()
    {
        return ResolveAttackRangeWorldUnits() * enemyData.RetreatTriggerRangeRatio;
    }

    private float GetRetreatCompleteDistance()
    {
        return ResolveAttackRangeWorldUnits() * enemyData.RetreatCompleteRangeRatio;
    }

    private float ResolveAttackRangeWorldUnits()
    {
        return PropValueUtility.DistancePointsToEffectiveAttackRangeWorldUnits(
            propertiesManager.GetPropValue(PropType.AttackRange));
    }

    private float GetDistanceToTarget()
    {
        return target != null ? Vector2.Distance(owner.Center, target.Center) : float.PositiveInfinity;
    }

    private void RequestStateByCombatContext()
    {
        if (target == null)
        {
            stateMachine.ChangeState(KitingRangedAIState.Idle);
            return;
        }

        if (ShouldRetreat())
        {
            stateMachine.ChangeState(KitingRangedAIState.Retreat);
            return;
        }

        if (!attackStrategy.IsTargetInRange(target))
        {
            stateMachine.ChangeState(KitingRangedAIState.Approach);
            return;
        }

        if (attackStrategy.CanUse(target))
        {
            stateMachine.ChangeState(KitingRangedAIState.Attack);
            return;
        }

        stateMachine.ChangeState(KitingRangedAIState.Idle);
    }

    private sealed class IdleState : StateBase<KitingRangedAIState>
    {
        private readonly KitingRangedEnemyBrain brain;

        public IdleState(KitingRangedEnemyBrain brain) : base(KitingRangedAIState.Idle)
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
            brain.RequestStateByCombatContext();
        }
    }

    private sealed class ApproachState : StateBase<KitingRangedAIState>
    {
        private readonly KitingRangedEnemyBrain brain;

        public ApproachState(KitingRangedEnemyBrain brain) : base(KitingRangedAIState.Approach)
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
            brain.RequestStateByCombatContext();
        }

        public override void OnFixedUpdate()
        {
            if (brain.target == null)
            {
                brain.currentMovable.StopMoving();
                return;
            }

            brain.approachMoveStrategy.ExecuteMove(brain.target);
            brain.FaceTarget();
        }
    }

    private sealed class RetreatState : StateBase<KitingRangedAIState>
    {
        private readonly KitingRangedEnemyBrain brain;

        public RetreatState(KitingRangedEnemyBrain brain) : base(KitingRangedAIState.Retreat)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.currentAnimatable.PlayState(brain.enemyData.AnimConfig.MoveHash);
        }

        public override void OnUpdate()
        {
            if (brain.target == null)
            {
                brain.stateMachine.ChangeState(KitingRangedAIState.Idle);
                return;
            }

            if (brain.HasReachedRetreatSafeDistance())
            {
                brain.RequestStateByCombatContext();
            }
        }

        public override void OnFixedUpdate()
        {
            if (brain.target == null)
            {
                brain.currentMovable.StopMoving();
                return;
            }

            brain.retreatMoveStrategy.ExecuteMove(brain.target);
            brain.FaceMoveDirection();
        }
    }

    private sealed class AttackState : EnemyActionStateBase<KitingRangedAIState>
    {
        private readonly KitingRangedEnemyBrain brain;
        private bool attackFinished;

        public AttackState(KitingRangedEnemyBrain brain) : base(KitingRangedAIState.Attack)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            attackFinished = false;

            if (brain.target == null)
            {
                brain.stateMachine.RequestState(KitingRangedAIState.Idle, StateChangeMode.Force);
                return;
            }

            if (!brain.attackStrategy.CanUse(brain.target))
            {
                brain.stateMachine.RequestState(KitingRangedAIState.Idle, StateChangeMode.Force);
                return;
            }

            brain.FaceTarget();
            brain.currentMovable.StopMoving();
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
            if (!attackFinished && brain.attackStrategy.TryExecuteCommitted(brain.target))
            {
                attackFinished = true;
            }
        }

        protected override void OnActionComplete()
        {
            brain.RequestStateByCombatContext();
        }
    }
}
