using System;
using UnityEngine;

[RequireComponent(typeof(RangeAttackComponent))]
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

    private IAttackable rangeAttack;
    private WormEnemySO enemyData;
    private MovementStrategyBase currentMoveStrategy;
    private AttackStrategyBase currentAttackStrategy;

    protected override void OnInitialize(Entity owner)
    {
        base.OnInitialize(owner);

        rangeAttack = owner.GetComponent<RangeAttackComponent>();
        enemyData = this.owner.EnemyData as WormEnemySO;

        if (rangeAttack == null)
        {
            throw new MissingComponentException($"{nameof(WormBrain)} requires a {nameof(RangeAttackComponent)}.");
        }

        if (enemyData == null)
        {
            throw new ArgumentException($"{nameof(WormBrain)} requires an {nameof(WormEnemySO)} definition.", nameof(owner));
        }
    }

    protected override void OnBrainStart()
    {
        ValidateStrategies();
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

    private void ValidateStrategies()
    {
        if (enemyData.approachMoveStrategy == null)
        {
            throw new MissingReferenceException($"{nameof(WormEnemySO)} on {enemyData.name} is missing {nameof(enemyData.approachMoveStrategy)}.");
        }

        if (enemyData.retreatMoveStrategy == null)
        {
            throw new MissingReferenceException($"{nameof(WormEnemySO)} on {enemyData.name} is missing {nameof(enemyData.retreatMoveStrategy)}.");
        }

        if (enemyData.attackStrategy == null)
        {
            throw new MissingReferenceException($"{nameof(WormEnemySO)} on {enemyData.name} is missing {nameof(enemyData.attackStrategy)}.");
        }

        if (enemyData.retreatAttackStrategy == null)
        {
            throw new MissingReferenceException($"{nameof(WormEnemySO)} on {enemyData.name} is missing {nameof(enemyData.retreatAttackStrategy)}.");
        }
    }

    private void SetMoveStrategy(MovementStrategyBase strategy)
    {
        currentMoveStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    private void SetAttackStrategy(AttackStrategyBase strategy)
    {
        currentAttackStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
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
            
            float distanceToTarget = brain.GetDistanceToTarget();
            if (distanceToTarget < brain.enemyData.retreatTriggerDistance)
            {
                brain.stateMachine.ChangeState(WormAIState.Retreat);
                return;
            }

            if (brain.rangeAttack.CanAttack && brain.rangeAttack.IsInAttackRange(brain.target))
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
            brain.SetMoveStrategy(brain.enemyData.approachMoveStrategy);
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

            if (brain.rangeAttack.CanAttack && brain.rangeAttack.IsInAttackRange(brain.target))
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

            brain.currentMoveStrategy.ExecuteMove(brain.currentMovable, brain.owner, brain.target, brain.enemyData);
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
            brain.SetMoveStrategy(brain.enemyData.retreatMoveStrategy);
            brain.SetAttackStrategy(brain.enemyData.retreatAttackStrategy);
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

            if (brain.rangeAttack.CanAttack && brain.rangeAttack.IsInAttackRange(brain.target))
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

            brain.currentMoveStrategy.ExecuteMove(brain.currentMovable, brain.owner, brain.target, brain.enemyData);
            brain.FaceMoveDirection();
        }
    }

    private sealed class AttackState : StateBase<WormAIState>
    {
        private readonly WormBrain brain;

        private bool attackCommitted = false;
        private bool attackFinished = false;

        public AttackState(WormBrain brain) : base(WormAIState.Attack)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            attackCommitted = false;
            attackFinished = false;
            brain.SetAttackStrategy(brain.enemyData.attackStrategy);
            brain.FaceTarget();
            brain.currentAnimatable.PlayState(brain.enemyData.AnimConfig.AttackHash);
            brain.currentMovable.StopMoving();
        }

        public override void OnUpdate()
        {
            brain.FaceTarget();

            if (brain.target == null)
            {
                brain.stateMachine.ChangeState(WormAIState.Idle);
                return;
            }
            
            float normalizedTime = brain.currentAnimatable.GetCurrentStateNormalizedTime();
            if (!attackCommitted && normalizedTime >= brain.enemyData.attackCommitNormalizedTime)
            {
                attackCommitted = true;

                if (!attackFinished && brain.rangeAttack.CanAttack)
                {
                    brain.currentAttackStrategy.ExecuteAttack(brain.rangeAttack, brain.owner, brain.target);
                    attackFinished = true;
                }
            }

            if (normalizedTime < brain.enemyData.attackFinishNormalizedTime) return;
            

            float distanceToTarget = brain.GetDistanceToTarget();
            if (distanceToTarget < brain.enemyData.retreatTriggerDistance)
            {
                brain.stateMachine.ChangeState(WormAIState.Retreat);
            }
            else if (!brain.rangeAttack.IsInAttackRange(brain.target))
            {
                brain.stateMachine.ChangeState(WormAIState.Approach);
            }
            else
            {
                brain.stateMachine.ChangeState(WormAIState.Idle);
            }
        }
    }
}
