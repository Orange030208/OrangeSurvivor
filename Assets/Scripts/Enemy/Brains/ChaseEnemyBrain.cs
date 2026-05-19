using System;
using UnityEngine;

[RequireComponent(typeof(EnemyAttackController))]
public class ChaseEnemyBrain : EnemyBrain
{
    private const string ATTACK_MOVE_MODIFIER_SOURCE = "ChaseEnemyBrain_AttackMove";

    public enum ChaseEnemyAIState
    {
        Idle,
        Chase,
        Attack
    }

    private readonly StateMachine<ChaseEnemyAIState> stateMachine = new();

    [Header("攻击点位")]
    [SerializeField] private Transform meleePointTransform;

    private EnemyAttackController attackController;
    private SkeletonEnemySO enemyData;
    private IMoveStrategy chaseMoveStrategy;
    private IAttackStrategy attackStrategy;
    private bool attackMoveModifiersApplied;

    protected override void OnInitialize(Entity owner)
    {
        base.OnInitialize(owner);

        attackController = owner.GetComponent<EnemyAttackController>();
        enemyData = this.owner.EnemyData as SkeletonEnemySO;

        if (attackController == null)
        {
            throw new MissingComponentException($"{nameof(ChaseEnemyBrain)} requires an {nameof(EnemyAttackController)}.");
        }

        if (enemyData == null)
        {
            throw new ArgumentException($"{nameof(ChaseEnemyBrain)} requires a {nameof(SkeletonEnemySO)} definition.", nameof(owner));
        }
    }

    protected override void OnBrainStart()
    {
        BuildRuntimeStrategies();
        RegisterStates();
        stateMachine.ChangeState(ChaseEnemyAIState.Chase);
    }

    protected override void OnBrainUpdate()
    {
        stateMachine.Update();
    }

    protected override void OnBrainFixedUpdate()
    {
        stateMachine.FixedUpdate();
    }

    public override void StopBrain()
    {
        RemoveAttackMoveModifiers();
        base.StopBrain();
    }

    public override void StartBrain()
    {
        RemoveAttackMoveModifiers();
        base.StartBrain();

        if (stateMachine.HasState)
        {
            stateMachine.ChangeState(ChaseEnemyAIState.Chase, true);
        }
    }

    public override void OnDisableComponent()
    {
        RemoveAttackMoveModifiers();
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
        attackStrategy = new DirectDamageAttackStrategy(
            owner,
            attackController,
            propertiesManager,
            enemyData.AttackAction.ActionId,
            enemyData.AttackSpeedBenefitRatio,
            meleePointTransform);
    }

    private bool CanUseAttack(Entity target)
    {
        return attackStrategy.CanUse(target);
    }

    private void ApplyAttackMoveModifiers()
    {
        if (attackMoveModifiersApplied)
        {
            return;
        }

        propertiesManager.AddModifiers(ATTACK_MOVE_MODIFIER_SOURCE, enemyData.AttackStateMoveModifiers);
        attackMoveModifiersApplied = true;
    }

    private void RemoveAttackMoveModifiers()
    {
        if (!attackMoveModifiersApplied)
        {
            return;
        }

        propertiesManager.RemoveModifiers(ATTACK_MOVE_MODIFIER_SOURCE);
        attackMoveModifiersApplied = false;
    }

    private void RequestIdleOrChaseAfterAttack()
    {
        if (target == null)
        {
            stateMachine.RequestState(ChaseEnemyAIState.Idle);
            return;
        }

        stateMachine.RequestState(attackStrategy.IsTargetInRange(target)
            ? ChaseEnemyAIState.Idle
            : ChaseEnemyAIState.Chase);
    }

    private sealed class IdleState : StateBase<ChaseEnemyAIState>
    {
        private readonly ChaseEnemyBrain brain;

        public IdleState(ChaseEnemyBrain brain) : base(ChaseEnemyAIState.Idle)
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

            if (!brain.attackStrategy.IsTargetInRange(brain.target))
            {
                brain.stateMachine.ChangeState(ChaseEnemyAIState.Chase);
                return;
            }

            if (brain.CanUseAttack(brain.target))
            {
                brain.stateMachine.ChangeState(ChaseEnemyAIState.Attack);
            }
        }
    }

    private sealed class ChaseState : StateBase<ChaseEnemyAIState>
    {
        private readonly ChaseEnemyBrain brain;

        public ChaseState(ChaseEnemyBrain brain) : base(ChaseEnemyAIState.Chase)
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
                brain.stateMachine.ChangeState(ChaseEnemyAIState.Idle);
                return;
            }

            if (brain.CanUseAttack(brain.target))
            {
                brain.stateMachine.ChangeState(ChaseEnemyAIState.Attack);
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

    private sealed class AttackState : EnemyActionStateBase<ChaseEnemyAIState>
    {
        private readonly ChaseEnemyBrain brain;

        public AttackState(ChaseEnemyBrain brain) : base(ChaseEnemyAIState.Attack)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.currentMovable.StopMoving();
            brain.ApplyAttackMoveModifiers();

            if (brain.target == null)
            {
                brain.stateMachine.RequestState(ChaseEnemyAIState.Idle, StateChangeMode.Force);
                return;
            }

            if (!brain.CanUseAttack(brain.target))
            {
                brain.stateMachine.RequestState(ChaseEnemyAIState.Chase, StateChangeMode.Force);
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

        public override void OnExit()
        {
            brain.RemoveAttackMoveModifiers();
            base.OnExit();
        }

        protected override void OnActionCommit()
        {
            brain.attackStrategy.TryExecuteCommitted(brain.target);
        }

        protected override void OnActionComplete()
        {
            brain.RequestIdleOrChaseAfterAttack();
        }
    }
}
