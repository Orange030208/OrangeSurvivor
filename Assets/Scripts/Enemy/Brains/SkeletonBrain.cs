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
    private MovementStrategyBase currentMoveStrategy;

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
        ValidateConfig();
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

    private void ValidateConfig()
    {
        if (enemyData.chaseMoveStrategy == null)
        {
            throw new MissingReferenceException($"{nameof(SkeletonEnemySO)} on {enemyData.name} is missing {nameof(enemyData.chaseMoveStrategy)}.");
        }

        if (enemyData.AttackDefinition == null)
        {
            throw new MissingReferenceException($"{nameof(SkeletonEnemySO)} on {enemyData.name} is missing {nameof(enemyData.AttackDefinition)}.");
        }
    }

    private void SetMoveStrategy(MovementStrategyBase strategy)
    {
        currentMoveStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
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

            bool isTargetInRange = brain.attackController.IsInAttackRange(brain.enemyData.AttackDefinition, brain.target);
            if (!isTargetInRange)
            {
                brain.stateMachine.ChangeState(SkeletonAIState.Chase);
                return;
            }
            
            if (brain.attackController.CanUse(brain.enemyData.AttackDefinition))
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
            brain.SetMoveStrategy(brain.enemyData.chaseMoveStrategy);
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

            if (brain.attackController.CanUse(brain.enemyData.AttackDefinition) &&
                brain.attackController.IsInAttackRange(brain.enemyData.AttackDefinition, brain.target))
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

            brain.currentMoveStrategy.ExecuteMove(brain.currentMovable, brain.owner, brain.target, brain.enemyData);
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

            if (!brain.attackController.CanUse(brain.enemyData.AttackDefinition) ||
                !brain.attackController.IsInAttackRange(brain.enemyData.AttackDefinition, brain.target))
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

                if (brain.attackController.CanUse(brain.enemyData.AttackDefinition) &&
                    brain.attackController.IsInAttackRange(brain.enemyData.AttackDefinition, brain.target))
                {
                    brain.attackController.TryUse(brain.enemyData.AttackDefinition, brain.target);
                }
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

            if (brain.attackController.IsInAttackRange(brain.enemyData.AttackDefinition, brain.target))
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
