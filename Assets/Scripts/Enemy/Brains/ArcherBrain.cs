using System;
using UnityEngine;

[RequireComponent(typeof(RangeAttackComponent))]
public class ArcherBrain : EnemyBrain
{
    public enum ArcherAIState
    {
        Idle,
        Approach,
        Retreat,
        Attack
    }

    private readonly StateMachine<ArcherAIState> stateMachine = new();

    private IAttackable rangeAttack;
    private ArcherEnemySO enemyData;
    private MovementStrategyBase currentMoveStrategy;
    private AttackStrategyBase currentAttackStrategy;

    protected override void OnInitialize(Entity owner)
    {
        base.OnInitialize(owner);

        rangeAttack = owner.GetComponent<RangeAttackComponent>();
        enemyData = this.owner.EnemyData as ArcherEnemySO;

        if (rangeAttack == null)
        {
            throw new MissingComponentException($"{nameof(ArcherBrain)} requires a {nameof(RangeAttackComponent)}.");
        }

        if (enemyData == null)
        {
            throw new ArgumentException($"{nameof(ArcherBrain)} requires an {nameof(ArcherEnemySO)} definition.", nameof(owner));
        }
    }

    protected override void OnBrainStart()
    {
        ValidateStrategies();
        RegisterStates();
        stateMachine.ChangeState(ArcherAIState.Approach);
    }

    protected override void OnDetermineState()
    {
        if (target == null)
        {
            stateMachine.ChangeState(ArcherAIState.Idle);
            return;
        }

        float distanceToTarget = Vector2.Distance(owner.Center, target.Center);
        bool shouldKeepRetreating = stateMachine.IsCurrentState(ArcherAIState.Retreat) &&
                                    distanceToTarget < enemyData.retreatCompleteDistance;

        if (distanceToTarget < enemyData.retreatTriggerDistance || shouldKeepRetreating)
        {
            stateMachine.ChangeState(ArcherAIState.Retreat);
            return;
        }

        if (rangeAttack.IsInAttackRange(target))
        {
            stateMachine.ChangeState(ArcherAIState.Attack);
            return;
        }

        stateMachine.ChangeState(ArcherAIState.Approach);
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
            throw new MissingReferenceException($"{nameof(ArcherEnemySO)} on {enemyData.name} is missing {nameof(enemyData.approachMoveStrategy)}.");
        }

        if (enemyData.retreatMoveStrategy == null)
        {
            throw new MissingReferenceException($"{nameof(ArcherEnemySO)} on {enemyData.name} is missing {nameof(enemyData.retreatMoveStrategy)}.");
        }

        if (enemyData.attackStrategy == null)
        {
            throw new MissingReferenceException($"{nameof(ArcherEnemySO)} on {enemyData.name} is missing {nameof(enemyData.attackStrategy)}.");
        }

        if (enemyData.retreatAttackStrategy == null)
        {
            throw new MissingReferenceException($"{nameof(ArcherEnemySO)} on {enemyData.name} is missing {nameof(enemyData.retreatAttackStrategy)}.");
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

    private sealed class IdleState : StateBase<ArcherAIState>
    {
        private readonly ArcherBrain brain;

        public IdleState(ArcherBrain brain) : base(ArcherAIState.Idle)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.currentMovable.StopMoving();
        }
    }

    private sealed class ApproachState : StateBase<ArcherAIState>
    {
        private readonly ArcherBrain brain;

        public ApproachState(ArcherBrain brain) : base(ArcherAIState.Approach)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.SetMoveStrategy(brain.enemyData.approachMoveStrategy);
        }

        public override void OnFixedUpdate()
        {
            brain.currentMoveStrategy.ExecuteMove(brain.currentMovable, brain.owner, brain.target, brain.enemyData);
        }
    }

    private sealed class RetreatState : StateBase<ArcherAIState>
    {
        private readonly ArcherBrain brain;

        public RetreatState(ArcherBrain brain) : base(ArcherAIState.Retreat)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.SetMoveStrategy(brain.enemyData.retreatMoveStrategy);
            brain.SetAttackStrategy(brain.enemyData.retreatAttackStrategy);
        }

        public override void OnFixedUpdate()
        {
            brain.currentMoveStrategy.ExecuteMove(brain.currentMovable, brain.owner, brain.target, brain.enemyData);
            brain.currentAttackStrategy.ExecuteAttack(brain.rangeAttack, brain.owner, brain.target);
        }
    }

    private sealed class AttackState : StateBase<ArcherAIState>
    {
        private readonly ArcherBrain brain;

        public AttackState(ArcherBrain brain) : base(ArcherAIState.Attack)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.SetAttackStrategy(brain.enemyData.attackStrategy);
            brain.currentMovable.StopMoving();
        }

        public override void OnFixedUpdate()
        {
            brain.currentMovable.StopMoving();
            if (brain.rangeAttack.IsInAttackRange(brain.target))
            {
                brain.currentAttackStrategy.ExecuteAttack(brain.rangeAttack, brain.owner, brain.target);
            }
        }
    }
}
