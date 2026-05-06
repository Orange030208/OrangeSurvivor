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

    private EnemyAttackController attackController;
    private SkeletonEnemySO enemyData;
    private IEnemyRuntimeMovementStrategy chaseMoveStrategy;
    private IEnemyRuntimeAttackStrategy attackStrategy;

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
        chaseMoveStrategy = EnemyRuntimeStrategyFactory.CreateMovementStrategy(owner, currentMovable, propertiesManager, enemyData.ChaseMovement);
        IEnemyRuntimeDetectionStrategy detectionStrategy = EnemyRuntimeStrategyFactory.CreateForwardCircleDetectionStrategy(owner, propertiesManager, enemyData.AttackConfig);
        attackStrategy = EnemyRuntimeStrategyFactory.CreateDirectDamageAttackStrategy(owner, attackController, propertiesManager, enemyData.AttackConfig, detectionStrategy);
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
        private bool attackStarted;
        private bool attackCommitted;
        private int attackStateHash;

        public AttackState(SkeletonBrain brain) : base(SkeletonAIState.Attack)
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
                brain.stateMachine.ChangeState(SkeletonAIState.Idle);
                return;
            }

            if (!brain.attackStrategy.CanUse(brain.target))
            {
                brain.stateMachine.ChangeState(SkeletonAIState.Chase);
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
                brain.stateMachine.ChangeState(SkeletonAIState.Idle);
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

                brain.attackStrategy.TryExecute(brain.target);
            }

            if (normalizedTime >= brain.enemyData.AttackFinishNormalizedTime)
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
