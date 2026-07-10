using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyAttackController))]
public class ChargerEnemyBrain : EnemyBrain
{
    private const string CHARGE_MODIFIER_SOURCE = "ChargerEnemyBrain_Charge";
    private const int CHARGE_HIT_BUFFER_SIZE = 32;

    public enum ChargerAIState
    {
        Idle,
        Chase,
        Attack,
        ChargeWindup,
        Charge
    }

    private readonly StateMachine<ChargerAIState> stateMachine = new();
    private readonly HashSet<Entity> chargeHitTargets = new();
    private readonly Collider2D[] chargeHitBuffer = new Collider2D[CHARGE_HIT_BUFFER_SIZE];

    [Header("攻击点位")]
    [SerializeField] private Transform meleePointTransform;

    private EnemyAttackController attackController;
    private ChargerEnemySO enemyData;
    private IMoveStrategy chaseMoveStrategy;
    private IAttackStrategy attackStrategy;
    private float chargeTimer;
    private Vector2 chargeDirection = Vector2.right;
    private bool chargeModifiersApplied;

    protected override void OnInitialize(Entity owner)
    {
        base.OnInitialize(owner);

        attackController = owner.GetComponent<EnemyAttackController>();
        enemyData = this.owner.EnemyData as ChargerEnemySO;

        if (attackController == null)
        {
            throw new MissingComponentException($"{nameof(ChargerEnemyBrain)} requires an {nameof(EnemyAttackController)}.");
        }

        if (enemyData == null)
        {
            throw new ArgumentException($"{nameof(ChargerEnemyBrain)} requires a {nameof(ChargerEnemySO)} definition.", nameof(owner));
        }
    }

    protected override void OnBrainStart()
    {
        BuildRuntimeStrategies();
        RegisterStates();
        ResetChargeTimer();
        stateMachine.ChangeState(ChargerAIState.Chase);
    }

    protected override void OnBrainUpdate()
    {
        TickChargeTimer();
        stateMachine.Update();
    }

    protected override void OnBrainFixedUpdate()
    {
        stateMachine.FixedUpdate();
    }

    public override void StopBrain()
    {
        RemoveChargeModifiers();
        base.StopBrain();
    }

    public override void StartBrain()
    {
        bool shouldResetExistingState = HasBrainStarted;
        RemoveChargeModifiers();
        ResetChargeTimer();
        base.StartBrain();

        if (shouldResetExistingState && stateMachine.HasState)
        {
            stateMachine.ChangeState(ChargerAIState.Chase, true);
        }
    }

    public override void OnDisableComponent()
    {
        RemoveChargeModifiers();
    }

    private void RegisterStates()
    {
        stateMachine.RegisterState(new IdleState(this));
        stateMachine.RegisterState(new ChaseState(this));
        stateMachine.RegisterState(new AttackState(this));
        stateMachine.RegisterState(new ChargeWindupState(this));
        stateMachine.RegisterState(new ChargeState(this));
    }

    private void BuildRuntimeStrategies()
    {
        chaseMoveStrategy = new DirectChaseMoveStrategy(currentMovable);
        attackStrategy = new DirectDamageAttackStrategy(
            owner,
            attackController,
            AttributeManager,
            ChargerEnemySO.ATTACK_ACTION_ID,
            enemyData.AttackSpeedBenefitRatio,
            meleePointTransform);
    }

    private void TickChargeTimer()
    {
        if (IsInChargeSequence())
        {
            return;
        }

        chargeTimer -= Time.deltaTime;
    }

    private bool IsChargeReady()
    {
        return target != null && chargeTimer <= 0f && !IsInChargeSequence();
    }

    private bool IsInChargeSequence()
    {
        return stateMachine.IsCurrentState(ChargerAIState.ChargeWindup) ||
               stateMachine.IsCurrentState(ChargerAIState.Charge);
    }

    private void ResetChargeTimer()
    {
        chargeTimer = enemyData != null ? enemyData.ChargeInterval : 0f;
    }

    private void ApplyChargeModifiers()
    {
        if (chargeModifiersApplied)
        {
            return;
        }

        AttributeManager.AddModifiers(CHARGE_MODIFIER_SOURCE, enemyData.ChargeModifiers);
        chargeModifiersApplied = true;
    }

    private void RemoveChargeModifiers()
    {
        if (!chargeModifiersApplied)
        {
            return;
        }

        AttributeManager.RemoveModifiers(CHARGE_MODIFIER_SOURCE);
        chargeModifiersApplied = false;
    }

    private void CaptureChargeDirection()
    {
        Vector2 direction = target != null ? target.Center - owner.Center : currentMovable.MoveDirection;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            direction = owner.transform.localScale.x < 0f ? Vector2.left : Vector2.right;
        }

        chargeDirection = direction.normalized;
    }

    private ChargerAIState ResolveStateAfterCharge()
    {
        if (target == null)
        {
            return ChargerAIState.Chase;
        }

        if (attackStrategy.CanUse(target))
        {
            return ChargerAIState.Attack;
        }

        return attackStrategy.IsTargetInRange(target)
            ? ChargerAIState.Idle
            : ChargerAIState.Chase;
    }

    private void RequestIdleOrChaseAfterAttack()
    {
        if (target == null)
        {
            stateMachine.RequestState(ChargerAIState.Idle);
            return;
        }

        stateMachine.RequestState(attackStrategy.IsTargetInRange(target)
            ? ChargerAIState.Idle
            : ChargerAIState.Chase);
    }

    private void DealChargeDamage()
    {
        if (enemyData.ChargeDamageRadius <= 0f)
        {
            return;
        }

        int hitCount = Physics2D.OverlapCircleNonAlloc(owner.Center, enemyData.ChargeDamageRadius, chargeHitBuffer, attackController.AttackLayer);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = chargeHitBuffer[i];
            if (hitCollider == null)
            {
                continue;
            }

            Entity hitEntity = hitCollider.GetComponent<Entity>();
            if (hitEntity == null)
            {
                hitEntity = hitCollider.GetComponentInParent<Entity>();
            }

            if (hitEntity == null || hitEntity == owner || chargeHitTargets.Contains(hitEntity))
            {
                continue;
            }

            chargeHitTargets.Add(hitEntity);
            float damage = PropValueUtility.ClampNonNegative(
                AttributeManager.GetAttributeValue(PropType.Attack) * enemyData.ChargeDamageMultiplier);
            Vector2 knockbackDirection = hitEntity.Center - owner.Center;
            HitService.Apply(new HitRequest(
                owner,
                hitEntity,
                HitSpec.EnemyHitSpec(damage),
                hitEntity.Center,
                knockbackDirection,
                HitSourceKind.Direct,
                sourcePosition: owner.Center));
        }
    }

    private sealed class IdleState : StateBase<ChargerAIState>
    {
        private readonly ChargerEnemyBrain brain;

        public IdleState(ChargerEnemyBrain brain) : base(ChargerAIState.Idle)
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

            if (brain.IsChargeReady())
            {
                brain.stateMachine.ChangeState(ChargerAIState.ChargeWindup);
                return;
            }

            if (!brain.attackStrategy.IsTargetInRange(brain.target))
            {
                brain.stateMachine.ChangeState(ChargerAIState.Chase);
                return;
            }

            if (brain.attackStrategy.CanUse(brain.target))
            {
                brain.stateMachine.ChangeState(ChargerAIState.Attack);
            }
        }
    }

    private sealed class ChaseState : StateBase<ChargerAIState>
    {
        private readonly ChargerEnemyBrain brain;

        public ChaseState(ChargerEnemyBrain brain) : base(ChargerAIState.Chase)
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
                brain.stateMachine.ChangeState(ChargerAIState.Idle);
                return;
            }

            if (brain.IsChargeReady())
            {
                brain.stateMachine.ChangeState(ChargerAIState.ChargeWindup);
                return;
            }

            if (brain.attackStrategy.CanUse(brain.target))
            {
                brain.stateMachine.ChangeState(ChargerAIState.Attack);
                return;
            }

            if (brain.attackStrategy.IsTargetInRange(brain.target))
            {
                brain.stateMachine.ChangeState(ChargerAIState.Idle);
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

    private sealed class AttackState : EnemyActionStateBase<ChargerAIState>
    {
        private readonly ChargerEnemyBrain brain;

        public AttackState(ChargerEnemyBrain brain) : base(ChargerAIState.Attack)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.currentMovable.StopMoving();

            if (brain.target == null)
            {
                brain.stateMachine.RequestState(ChargerAIState.Idle, StateChangeMode.Force);
                return;
            }

            if (!brain.attackStrategy.CanUse(brain.target))
            {
                brain.stateMachine.RequestState(ChargerAIState.Chase, StateChangeMode.Force);
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

        protected override void OnActionCommit()
        {
            brain.attackStrategy.TryExecuteCommitted(brain.target);
        }

        protected override void OnActionComplete()
        {
            if (brain.IsChargeReady())
            {
                brain.stateMachine.RequestState(ChargerAIState.ChargeWindup);
                return;
            }

            brain.RequestIdleOrChaseAfterAttack();
        }
    }

    private sealed class ChargeWindupState : EnemyActionStateBase<ChargerAIState>
    {
        private readonly ChargerEnemyBrain brain;

        public ChargeWindupState(ChargerEnemyBrain brain) : base(ChargerAIState.ChargeWindup)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.currentMovable.StopMoving();
            brain.FaceTarget();
            BeginAction(brain.enemyData.ChargeAction, brain.currentAnimatable);
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

        protected override void OnActionComplete()
        {
            brain.stateMachine.RequestState(ChargerAIState.Charge);
        }
    }

    private sealed class ChargeState : StateBase<ChargerAIState>
    {
        private readonly ChargerEnemyBrain brain;
        private float elapsedTime;

        public ChargeState(ChargerEnemyBrain brain) : base(ChargerAIState.Charge)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            elapsedTime = 0f;
            brain.chargeHitTargets.Clear();
            brain.CaptureChargeDirection();
            brain.ApplyChargeModifiers();
            brain.facingController?.FaceDirection(brain.chargeDirection);
            brain.DealChargeDamage();
        }

        public override void OnUpdate()
        {
            elapsedTime += Time.deltaTime;
            if (elapsedTime >= brain.enemyData.ChargeDuration)
            {
                brain.stateMachine.RequestState(brain.ResolveStateAfterCharge());
            }
        }

        public override void OnFixedUpdate()
        {
            brain.currentMovable.MoveTo(brain.owner.Center + brain.chargeDirection);
            brain.facingController?.FaceDirection(brain.chargeDirection);
            brain.DealChargeDamage();
        }

        public override void OnExit()
        {
            brain.currentMovable.StopMoving();
            brain.RemoveChargeModifiers();
            brain.ResetChargeTimer();
        }
    }
}
