using System;
using UnityEngine;

[RequireComponent(typeof(MeleeAttackComponent))]
public class SkeletonBrain : EnemyBrain
{
    public enum SkeletonAIState
    {
        Idle,
        Chase,
        Attack
    }

    private readonly StateMachine<SkeletonAIState> stateMachine = new();

    private IAttackable meleeAttack;
    private SkeletonEnemySO enemyData;
    private MovementStrategyBase currentMoveStrategy;

    protected override void OnInitialize(Entity owner)
    {
        base.OnInitialize(owner);

        meleeAttack = owner.GetComponent<MeleeAttackComponent>();
        enemyData = this.owner.EnemyData as SkeletonEnemySO;

        if (meleeAttack == null)
        {
            throw new MissingComponentException($"{nameof(SkeletonBrain)} requires a {nameof(MeleeAttackComponent)}.");
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
            bool isTargetInRange = brain.meleeAttack.IsInAttackRange(brain.target);
            
            if (brain.meleeAttack.CanAttack)
            {
                if (isTargetInRange)
                {
                    brain.stateMachine.ChangeState(SkeletonAIState.Attack);
                }
                else
                {
                    brain.stateMachine.ChangeState(SkeletonAIState.Chase);
                }
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
            if (brain.target == null)
            {
                brain.stateMachine.ChangeState(SkeletonAIState.Idle);
                return;
            }

            if (brain.meleeAttack.CanAttack && brain.meleeAttack.IsInAttackRange(brain.target))
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

            if (!brain.meleeAttack.CanAttack || !brain.meleeAttack.IsInAttackRange(brain.target))
            {
                brain.stateMachine.ChangeState(SkeletonAIState.Chase);
                return;
            }

            attackStarted = true;
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

                if (brain.meleeAttack.CanAttack && brain.meleeAttack.IsInAttackRange(brain.target))
                {
                    brain.meleeAttack.TryAttack(brain.target);
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

            if (brain.meleeAttack.IsInAttackRange(brain.target))
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
