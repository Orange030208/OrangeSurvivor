using System;
using UnityEngine;

[RequireComponent(typeof(EnemyAttackController))]
public class SkeletonBrain : EnemyBrain
{
    public enum SkeletonAIState
    {
        Idle,
        Chase,
        Attack
    }

    private readonly StateMachine<SkeletonAIState> stateMachine = new();

    [Header("Attack Points")]
    [SerializeField] private Transform meleePointTransform;

    private EnemyAttackController attackController;
    private SkeletonEnemySO enemyData;
    private IMoveStrategy chaseMoveStrategy;
    private IAttackStrategy attackStrategy;

    protected override void OnInitialize(Entity owner)
    {
        base.OnInitialize(owner);

        attackController = owner.GetComponent<EnemyAttackController>();
        enemyData = this.owner.EnemyData as SkeletonEnemySO;

        if (attackController == null)
        {
            throw new MissingComponentException($"{nameof(SkeletonBrain)} requires an {nameof(EnemyAttackController)}.");
        }

        if (enemyData == null)
        {
            throw new ArgumentException($"{nameof(SkeletonBrain)} requires a {nameof(SkeletonEnemySO)} definition.", nameof(owner));
        }
    }

    protected override void OnBrainStart()
    {
        BuildRuntimeStrategies();
        RegisterStates();
        stateMachine.ChangeState(SkeletonAIState.Chase);
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
        stateMachine.RegisterState(new ChaseState(this));
        stateMachine.RegisterState(new AttackState(this));
    }

    private void BuildRuntimeStrategies()
    {
        chaseMoveStrategy = new DirectChaseMoveStrategy(currentMovable);
        IRangeDetectionStrategy detectionStrategy = new DistanceRangeDetectionStrategy(owner, propertiesManager);
        attackStrategy = new DirectDamageAttackStrategy(
            owner,
            attackController,
            propertiesManager,
            SkeletonEnemySO.ATTACK_ACTION_ID,
            enemyData.AttackSpeedBenefitRatio,
            detectionStrategy,
            meleePointTransform);
    }

    private sealed class IdleState : StateBase<SkeletonAIState>
    {
        private readonly SkeletonBrain brain;

        public IdleState(SkeletonBrain brain) : base(SkeletonAIState.Idle)
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

            bool isTargetInRange = brain.attackStrategy.DetectionStrategy.IsTargetInRange(brain.target);
            if (!isTargetInRange)
            {
                brain.stateMachine.ChangeState(SkeletonAIState.Chase);
                return;
            }
            
            if (brain.attackStrategy.CanUse(brain.target))
            {
                brain.stateMachine.ChangeState(SkeletonAIState.Attack);
            }
        }
    }

    private sealed class ChaseState : StateBase<SkeletonAIState>
    {
        private readonly SkeletonBrain brain;

        public ChaseState(SkeletonBrain brain) : base(SkeletonAIState.Chase)
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
                brain.stateMachine.ChangeState(SkeletonAIState.Idle);
                return;
            }

            if (brain.attackStrategy.CanUse(brain.target))
            {
                brain.stateMachine.ChangeState(SkeletonAIState.Attack);
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

    private sealed class AttackState : StateBase<SkeletonAIState>
    {
        private readonly SkeletonBrain brain;
        private bool attackCommitted;

        public AttackState(SkeletonBrain brain) : base(SkeletonAIState.Attack)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            attackCommitted = false;
            brain.currentMovable.StopMoving();

            if (brain.target == null)
            {
                brain.stateMachine.ChangeState(SkeletonAIState.Idle);
                return;
            }

            if (!brain.attackStrategy.CanUse(brain.target))
            {
                brain.stateMachine.ChangeState(SkeletonAIState.Chase);
                return;
            }

            brain.FaceTarget();
            brain.currentAnimatable.PlayState(brain.enemyData.AnimConfig.AttackHash);
        }

        public override void OnUpdate()
        {
            if (brain.target == null)
            {
                brain.stateMachine.ChangeState(SkeletonAIState.Idle);
                return;
            }

            if (!brain.currentAnimatable.IsCurrentState(brain.enemyData.AnimConfig.AttackHash))
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
                brain.stateMachine.ChangeState(SkeletonAIState.Idle);
                return;
            }

            if (brain.attackStrategy.DetectionStrategy.IsTargetInRange(brain.target))
            {
                brain.stateMachine.ChangeState(SkeletonAIState.Idle);
            }
            else
            {
                brain.stateMachine.ChangeState(SkeletonAIState.Chase);
            }
        }
    }
}
