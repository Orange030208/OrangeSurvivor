using UnityEngine;

[RequireComponent(typeof(RangeAttackComponent))]
[RequireComponent(typeof(MeleeAttackComponent))]
public class MageBrain : EnemyBrain
{
    public enum MageAIState
    {
        Idle,
        CircleKite,
        RetreatBurst
    }

    private readonly StateMachine<MageAIState> stateMachine = new();

    private IAttackable rangeAttack;
    private IAttackable meleeAttack;
    private MageEnemySO enemyData;
    private MovementStrategyBase currentMoveStrategy;
    private AttackStrategyBase currentAttackStrategy;

    protected override void OnInitialize(Entity owner)
    {
        base.OnInitialize(owner);
        rangeAttack = owner.GetComponent<RangeAttackComponent>();
        meleeAttack = owner.GetComponent<MeleeAttackComponent>();
        enemyData = this.owner.EnemyData as MageEnemySO;
    }

    protected override void OnBrainStart()
    {
        RegisterStates();
        SetMoveStrategy(enemyData.normalMovementStrategy);
        SetAttackStrategy(enemyData.normalAttackStrategy);
        stateMachine.ChangeState(MageAIState.CircleKite);
    }

    protected override void OnDetermineState()
    {
        if (target == null)
        {
            stateMachine.ChangeState(MageAIState.Idle);
            return;
        }

        float hpPercent = healthComponent.CurrentHealth / healthComponent.MaxHealth * 100f;

        if (hpPercent <= enemyData.lowHpPercent)
        {
            stateMachine.ChangeState(MageAIState.RetreatBurst);
            return;
        }

        stateMachine.ChangeState(MageAIState.CircleKite);
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
        stateMachine.RegisterState(new CircleKiteState(this));
        stateMachine.RegisterState(new RetreatBurstState(this));
    }

    private void SetMoveStrategy(MovementStrategyBase strategy)
    {
        currentMoveStrategy = strategy;
    }

    private void SetAttackStrategy(AttackStrategyBase strategy)
    {
        currentAttackStrategy = strategy;
    }

    private sealed class IdleState : StateBase<MageAIState>
    {
        private readonly MageBrain brain;

        public IdleState(MageBrain brain) : base(MageAIState.Idle)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.currentMovable.StopMoving();
            brain.currentAnimatable.PlayState(brain.enemyData.AnimConfig.IdleHash);
        }
    }

    private sealed class CircleKiteState : StateBase<MageAIState>
    {
        private readonly MageBrain brain;

        public CircleKiteState(MageBrain brain) : base(MageAIState.CircleKite)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.SetMoveStrategy(brain.enemyData.normalMovementStrategy);
            brain.SetAttackStrategy(brain.enemyData.normalAttackStrategy);
            brain.currentAnimatable.PlayState(brain.enemyData.AnimConfig.MoveHash);
        }

        public override void OnFixedUpdate()
        {
            brain.currentMoveStrategy.ExecuteMove(brain.currentMovable, brain.owner, brain.target, brain.enemyData);
            if (brain.rangeAttack.IsInAttackRange(brain.target))
            {
                brain.currentAttackStrategy.ExecuteAttack(brain.rangeAttack, brain.owner, brain.target);
            }
        }
    }

    private sealed class RetreatBurstState : StateBase<MageAIState>
    {
        private const string RETREAT_BURST_MODIFIER_SOURCE = "MageBrain_RetreatBurst";
        private readonly MageBrain brain;

        public RetreatBurstState(MageBrain brain) : base(MageAIState.RetreatBurst)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.SetMoveStrategy(brain.enemyData.retreatMovementStrategy);
            brain.SetAttackStrategy(brain.enemyData.normalAttackStrategy);
            brain.currentAnimatable.PlayState(brain.enemyData.AnimConfig.MoveHash);
            brain.propertiesManager.AddModifiers(RETREAT_BURST_MODIFIER_SOURCE,brain.enemyData.fastBurstModifierData);
        }

        public override void OnFixedUpdate()
        {
            brain.currentMoveStrategy.ExecuteMove(brain.currentMovable, brain.owner, brain.target, brain.enemyData);
            brain.currentAttackStrategy.ExecuteAttack(brain.rangeAttack, brain.owner, brain.target);
        }

        public override void OnExit()
        {
            brain.propertiesManager.RemoveModifiers(RETREAT_BURST_MODIFIER_SOURCE);
        }
    }
}
