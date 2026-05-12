using System;
using UnityEngine;

[RequireComponent(typeof(EnemyAttackController))]
public class WormBrain : EnemyBrain
{
    public enum WormAIState
    {
        Idle,
        Approach,
        Retreat,
        Attack
    }

    private readonly StateMachine<WormAIState> stateMachine = new();

    [Header("攻击点位")]
    [SerializeField] private Transform attackPointTransform;
    [SerializeField] private Transform retreatAttackPointTransform;

    private EnemyAttackController attackController;
    private WormEnemySO enemyData;
    private IMoveStrategy currentMoveStrategy;
    private IMoveStrategy approachMoveStrategy;
    private IMoveStrategy retreatMoveStrategy;
    private IAttackStrategy currentAttackStrategy;
    private IAttackStrategy attackStrategy;
    private IAttackStrategy retreatAttackStrategy;

    protected override void OnInitialize(Entity owner)
    {
        base.OnInitialize(owner);

        attackController = owner.GetComponent<EnemyAttackController>();
        enemyData = this.owner.EnemyData as WormEnemySO;

        if (attackController == null)
        {
            throw new MissingComponentException($"{nameof(WormBrain)} requires an {nameof(EnemyAttackController)}.");
        }

        if (enemyData == null)
        {
            throw new ArgumentException($"{nameof(WormBrain)} requires an {nameof(WormEnemySO)} definition.", nameof(owner));
        }
    }

    protected override void OnBrainStart()
    {
        BuildRuntimeStrategies();
        RegisterStates();
        stateMachine.ChangeState(WormAIState.Approach);
    }

    protected override void OnBrainUpdate()
    {
        stateMachine.Update();
    }

    protected override void OnBrainFixedUpdate()
    {
        stateMachine.FixedUpdate();
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

        IRangeDetectionStrategy attackDetectionStrategy = new DistanceRangeDetectionStrategy(
            owner,
            propertiesManager);
        IRangeDetectionStrategy retreatAttackDetectionStrategy = new DistanceRangeDetectionStrategy(
            owner,
            propertiesManager);

        attackStrategy = new ProjectileAttackStrategy(
            owner,
            attackController,
            propertiesManager,
            WormEnemySO.ATTACK_ACTION_ID,
            enemyData.attackSpeedBenefitRatio,
            attackDetectionStrategy,
            attackPointTransform,
            enemyData.attackProjectileDefinition);
        retreatAttackStrategy = new ProjectileAttackStrategy(
            owner,
            attackController,
            propertiesManager,
            WormEnemySO.RETREAT_ATTACK_ACTION_ID,
            enemyData.retreatAttackSpeedBenefitRatio,
            retreatAttackDetectionStrategy,
            retreatAttackPointTransform,
            enemyData.retreatAttackProjectileDefinition);
        currentAttackStrategy = attackStrategy;
    }

    private void SetMoveStrategy(IMoveStrategy strategy)
    {
        currentMoveStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    private void SetAttackStrategy(IAttackStrategy attackStrategy)
    {
        currentAttackStrategy = attackStrategy ?? throw new ArgumentNullException(nameof(attackStrategy));
    }

    private float GetDistanceToTarget()
    {
        return target != null ? Vector2.Distance(owner.Center, target.Center) : float.PositiveInfinity;
    }

    private sealed class IdleState : StateBase<WormAIState>
    {
        private readonly WormBrain brain;

        public IdleState(WormBrain brain) : base(WormAIState.Idle)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.SetAttackStrategy(brain.attackStrategy);
            brain.currentMovable.StopMoving();
            brain.currentAnimatable.PlayState(brain.enemyData.AnimConfig.IdleHash);
        }

        public override void OnUpdate()
        {
            brain.FaceTarget();

            if (brain.target == null)
            {
                Debug.Log("丢失目标");
                return;
            }
            
            float distanceToTarget = brain.GetDistanceToTarget();
            if (distanceToTarget < brain.enemyData.retreatTriggerDistance)
            {
                brain.stateMachine.ChangeState(WormAIState.Retreat);
                return;
            }

            bool isTargetInRange = brain.attackStrategy.DetectionStrategy.IsTargetInRange(brain.target);
            if (!isTargetInRange)
            {
                brain.stateMachine.ChangeState(WormAIState.Approach);
                return;
            }

            if (brain.attackStrategy.CanUse(brain.target))
            {
                brain.stateMachine.ChangeState(WormAIState.Attack);
            }
        }
    }

    private sealed class ApproachState : StateBase<WormAIState>
    {
        private readonly WormBrain brain;

        public ApproachState(WormBrain brain) : base(WormAIState.Approach)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.SetAttackStrategy(brain.attackStrategy);
            brain.SetMoveStrategy(brain.approachMoveStrategy);
            brain.currentAnimatable.PlayState(brain.enemyData.AnimConfig.MoveHash);
        }

        public override void OnUpdate()
        {
            brain.FaceTarget();

            if (brain.target == null)
            {
                brain.stateMachine.ChangeState(WormAIState.Idle);
                return;
            }

            float distanceToTarget = brain.GetDistanceToTarget();
            if (distanceToTarget < brain.enemyData.retreatTriggerDistance)
            {
                brain.stateMachine.ChangeState(WormAIState.Retreat);
                return;
            }

            if (brain.attackStrategy.CanUse(brain.target))
            {
                brain.stateMachine.ChangeState(WormAIState.Attack);
            }
        }

        public override void OnFixedUpdate()
        {
            if (brain.target == null)
            {
                return;
            }

            brain.currentMoveStrategy.ExecuteMove(brain.target);
            brain.FaceTarget();
        }
    }

    private sealed class RetreatState : StateBase<WormAIState>
    {
        private readonly WormBrain brain;

        public RetreatState(WormBrain brain) : base(WormAIState.Retreat)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.SetMoveStrategy(brain.retreatMoveStrategy);
            brain.SetAttackStrategy(brain.retreatAttackStrategy);
            brain.currentAnimatable.PlayState(brain.enemyData.AnimConfig.MoveHash);
        }

        public override void OnUpdate()
        {
            if (brain.target == null)
            {
                brain.stateMachine.ChangeState(WormAIState.Idle);
                return;
            }

            float distanceToTarget = brain.GetDistanceToTarget();
            if (distanceToTarget < brain.enemyData.retreatCompleteDistance)
            {
                brain.FaceMoveDirection();
                return;
            }

            if (brain.currentAttackStrategy.CanUse(brain.target))
            {
                brain.stateMachine.ChangeState(WormAIState.Attack);
                return;
            }

            brain.stateMachine.ChangeState(WormAIState.Approach);
        }

        public override void OnFixedUpdate()
        {
            if (brain.target == null)
            {
                return;
            }

            brain.currentMoveStrategy.ExecuteMove(brain.target);
            brain.FaceMoveDirection();
        }
    }

    private sealed class AttackState : EnemyActionStateBase<WormAIState>
    {
        private readonly WormBrain brain;

        private bool attackFinished = false;

        public AttackState(WormBrain brain) : base(WormAIState.Attack)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            attackFinished = false;
            if (brain.currentAttackStrategy == null)
            {
                brain.SetAttackStrategy(brain.attackStrategy);
            }

            if (brain.target == null)
            {
                brain.stateMachine.RequestState(WormAIState.Idle, StateChangeMode.Force);
                return;
            }

            brain.FaceTarget();
            brain.currentMovable.StopMoving();
            BeginAction(ResolveAction(), brain.currentAnimatable);
        }

        public override void OnUpdate()
        {
            brain.FaceTarget();
            TickAction(Time.deltaTime);
        }

        protected override void OnActionCommit()
        {
            if (!attackFinished && brain.currentAttackStrategy.TryExecuteCommitted(brain.target))
            {
                attackFinished = true;
            }
        }

        protected override void OnActionComplete()
        {
            if (brain.target == null)
            {
                brain.stateMachine.RequestState(WormAIState.Idle);
                return;
            }

            float distanceToTarget = brain.GetDistanceToTarget();
            if (distanceToTarget < brain.enemyData.retreatTriggerDistance)
            {
                brain.stateMachine.RequestState(WormAIState.Retreat);
            }
            else if (!brain.currentAttackStrategy.DetectionStrategy.IsTargetInRange(brain.target))
            {
                brain.stateMachine.RequestState(WormAIState.Approach);
            }
            else
            {
                brain.stateMachine.RequestState(WormAIState.Idle);
            }
        }

        private EnemyActionDefinition ResolveAction()
        {
            return brain.currentAttackStrategy == brain.retreatAttackStrategy
                ? brain.enemyData.RetreatAttackAction
                : brain.enemyData.AttackAction;
        }
    }
}
