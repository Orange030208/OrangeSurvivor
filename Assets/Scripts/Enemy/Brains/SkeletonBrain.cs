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

    private const int AREA_HIT_BUFFER_SIZE = 16;

    private readonly StateMachine<SkeletonAIState> stateMachine = new();
    private readonly Collider2D[] areaHitBuffer = new Collider2D[AREA_HIT_BUFFER_SIZE];

    [Header("攻击点位")]
    [SerializeField] private Transform meleePointTransform;

    private EnemyAttackController attackController;
    private SkeletonEnemySO enemyData;
    private IMoveStrategy chaseMoveStrategy;
    private IAttackStrategy attackStrategy;
    private bool hasWarnedMissingMeleePoint;

    protected EnemyAttackController AttackController => attackController;
    protected SkeletonEnemySO EnemyData => enemyData;
    protected IAttackStrategy AttackStrategy => attackStrategy;

    protected override void OnInitialize(Entity owner)
    {
        base.OnInitialize(owner);

        attackController = owner.GetComponent<EnemyAttackController>();
        enemyData = ResolveEnemyData();

        if (attackController == null)
        {
            throw new MissingComponentException($"{GetType().Name} requires an {nameof(EnemyAttackController)}.");
        }

        if (enemyData == null)
        {
            throw new ArgumentException($"{GetType().Name} requires a {RequiredEnemyDataTypeName} definition.", nameof(owner));
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

    protected virtual SkeletonEnemySO ResolveEnemyData()
    {
        return owner.EnemyData as SkeletonEnemySO;
    }

    protected virtual string RequiredEnemyDataTypeName => nameof(SkeletonEnemySO);

    protected virtual void ResetAttackRuntime()
    {
    }

    protected virtual void OnAttackActionProgress(AnimationStateProgress progress)
    {
    }

    protected virtual void OnAttackActionCommit()
    {
        attackStrategy.TryExecuteCommitted(target);
    }

    protected virtual void OnAttackActionComplete()
    {
    }

    protected void ExecuteMeleeAreaAttack(float rangeMultiplier)
    {
        Vector2 attackCenter = ResolveMeleeAttackCenter();
        float attackRadius = PropValueUtility.DistancePointsToWorldUnits(propertiesManager.GetPropValue(PropType.AttackRange)) * Mathf.Max(0f, rangeMultiplier);

        int hitCount = Physics2D.OverlapCircleNonAlloc(
            attackCenter,
            attackRadius,
            areaHitBuffer,
            attackController.AttackLayer);

        for (int i = 0; i < hitCount; i++)
        {
            Entity hitEntity = ResolveEntity(areaHitBuffer[i]);
            if (hitEntity == null || hitEntity == owner)
            {
                continue;
            }

            Vector2 hitPoint = hitEntity.GetClosestPointTo(attackCenter);
            Vector2 knockbackDirection = hitEntity.Center - owner.Center;
            HitService.Apply(new HitRequest(
                owner,
                hitEntity,
                HitSpec.EnemyHitSpec(ResolveDamage()),
                hitPoint,
                knockbackDirection,
                HitSourceKind.Direct,
                owner.Center));
        }

    }

    protected void CommitAttackCooldown()
    {
        attackController.CommitBasicAttackCooldown(enemyData.AttackAction.ActionId);
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
            enemyData.AttackAction.ActionId,
            enemyData.AttackSpeedBenefitRatio,
            detectionStrategy,
            meleePointTransform);
    }

    private bool CanUseAttack(Entity target)
    {
        return attackStrategy.CanUse(target);
    }

    private Vector2 ResolveMeleeAttackCenter()
    {
        if (meleePointTransform != null)
        {
            return meleePointTransform.position;
        }

        if (!hasWarnedMissingMeleePoint)
        {
            hasWarnedMissingMeleePoint = true;
            Debug.LogWarning($"{GetType().Name} on {owner.name} is missing melee point. Falling back to owner center.", owner);
        }

        return owner.Center;
    }

    private float ResolveDamage()
    {
        return Mathf.Max(0f, propertiesManager.GetPropValue(PropType.Attack));
    }

    private void RequestIdleOrChaseAfterAttack()
    {
        if (target == null)
        {
            stateMachine.RequestState(SkeletonAIState.Idle);
            return;
        }

        stateMachine.RequestState(attackStrategy.DetectionStrategy.IsTargetInRange(target)
            ? SkeletonAIState.Idle
            : SkeletonAIState.Chase);
    }

    private static Entity ResolveEntity(Collider2D hitCollider)
    {
        if (hitCollider == null)
        {
            return null;
        }

        Entity entity = hitCollider.GetComponent<Entity>();
        return entity != null ? entity : hitCollider.GetComponentInParent<Entity>();
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

            if (brain.CanUseAttack(brain.target))
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

            if (brain.CanUseAttack(brain.target))
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
        private readonly EnemyActionRunner actionRunner = new();
        private bool completionHandled;

        public AttackState(SkeletonBrain brain) : base(SkeletonAIState.Attack)
        {
            this.brain = brain;
        }

        public override bool CanExitTo(SkeletonAIState nextState, StateChangeMode mode)
        {
            return mode == StateChangeMode.Force || actionRunner.IsComplete;
        }

        public override void OnEnter()
        {
            completionHandled = false;
            brain.ResetAttackRuntime();
            brain.currentMovable.StopMoving();

            if (brain.target == null)
            {
                brain.stateMachine.RequestState(SkeletonAIState.Idle, StateChangeMode.Force);
                return;
            }

            if (!brain.CanUseAttack(brain.target))
            {
                brain.stateMachine.RequestState(SkeletonAIState.Chase, StateChangeMode.Force);
                return;
            }

            brain.FaceTarget();
            actionRunner.Begin(brain.enemyData.AttackAction, brain.currentAnimatable);
        }

        public override void OnUpdate()
        {
            brain.FaceTarget();
            actionRunner.Tick(Time.deltaTime);

            if (actionRunner.ShouldCommit)
            {
                actionRunner.MarkCommitted();
                brain.OnAttackActionCommit();
            }

            brain.OnAttackActionProgress(actionRunner.Progress);

            if (actionRunner.IsComplete && !completionHandled)
            {
                completionHandled = true;
                brain.OnAttackActionComplete();
                brain.RequestIdleOrChaseAfterAttack();
            }
        }

        public override void OnFixedUpdate()
        {
            brain.currentMovable.StopMoving();
        }

        public override void OnExit()
        {
            completionHandled = false;
            actionRunner.Cancel();
        }
    }
}
