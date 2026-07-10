using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyAttackController))]
public sealed class CyberOrangeBossBrain : EnemyBrain
{
    private const string ATTACK_MOVE_MODIFIER_SOURCE = "CyberOrangeBossBrain_AttackMove";
    private const string CHARGE_MODIFIER_SOURCE = "CyberOrangeBossBrain_Charge";
    private const string ENRAGE_MODIFIER_SOURCE = "CyberOrangeBossBrain_Enrage";
    private const int CHARGE_HIT_BUFFER_SIZE = 32;

    private enum BossState
    {
        Idle,
        Chase,
        Attack,
        ChargeWindup,
        Charge,
        Barrage
    }

    private readonly StateMachine<BossState> stateMachine = new();
    private readonly HashSet<Entity> chargeHitTargets = new();
    private readonly Collider2D[] chargeHitBuffer = new Collider2D[CHARGE_HIT_BUFFER_SIZE];

    [Header("攻击点位")]
    [SerializeField] private Transform meleePointTransform;
    [SerializeField] private Transform shootPointTransform;

    private EnemyAttackController attackController;
    private CyberOrangeBossSO bossData;
    private IMoveStrategy chaseMoveStrategy;
    private IAttackStrategy meleeAttackStrategy;
    private bool attackMoveModifiersApplied;
    private bool chargeModifiersApplied;
    private bool enrageApplied;
    private Vector2 chargeDirection = Vector2.right;
    private int barrageShotsRemaining;
    private float barrageWindupTimer;
    private float barrageShotTimer;

    protected override void OnInitialize(Entity owner)
    {
        base.OnInitialize(owner);

        attackController = owner.GetComponent<EnemyAttackController>();
        bossData = this.owner.EnemyData as CyberOrangeBossSO;

        if (attackController == null)
        {
            throw new MissingComponentException($"{nameof(CyberOrangeBossBrain)} requires an {nameof(EnemyAttackController)}.");
        }

        if (bossData == null)
        {
            throw new MissingReferenceException($"{nameof(CyberOrangeBossBrain)} requires a {nameof(CyberOrangeBossSO)} definition.");
        }

        if (bossData.BarrageProjectileDefinition == null)
        {
            throw new MissingReferenceException(
                $"{nameof(CyberOrangeBossBrain)} requires {nameof(CyberOrangeBossSO)}.{nameof(CyberOrangeBossSO.BarrageProjectileDefinition)}.");
        }
    }

    protected override void OnBrainStart()
    {
        BuildRuntimeStrategies();
        RegisterStates();
        ResetRuntimeState();
        stateMachine.ChangeState(BossState.Chase, true);
    }

    protected override void OnBrainUpdate()
    {
        TryApplyEnrage();
        stateMachine.Update();
    }

    protected override void OnBrainFixedUpdate()
    {
        stateMachine.FixedUpdate();
    }

    public override void StopBrain()
    {
        ClearRuntimeModifiers();
        base.StopBrain();
    }

    public override void StartBrain()
    {
        bool shouldResetExistingState = HasBrainStarted;
        ClearRuntimeModifiers();
        ResetRuntimeState();
        base.StartBrain();

        if (shouldResetExistingState && stateMachine.HasState)
        {
            stateMachine.ChangeState(BossState.Chase, true);
        }
    }

    public override void OnDisableComponent()
    {
        ClearRuntimeModifiers();
    }

    private void RegisterStates()
    {
        stateMachine.RegisterState(new IdleState(this));
        stateMachine.RegisterState(new ChaseState(this));
        stateMachine.RegisterState(new AttackState(this));
        stateMachine.RegisterState(new ChargeWindupState(this));
        stateMachine.RegisterState(new ChargeState(this));
        stateMachine.RegisterState(new BarrageState(this));
    }

    private void BuildRuntimeStrategies()
    {
        chaseMoveStrategy = new DirectChaseMoveStrategy(currentMovable);
        meleeAttackStrategy = new DirectDamageAttackStrategy(
            owner,
            attackController,
            AttributeManager,
            bossData.AttackAction.ActionId,
            bossData.AttackSpeedBenefitRatio,
            meleePointTransform,
            bossData.AttackRangeMultiplier,
            null,
            bossData.AttackHitShape,
            ResolveFacingDirection,
            ResolveRangeDirection);
    }

    private void ResetRuntimeState()
    {
        currentMovable?.StopMoving();
        chargeHitTargets.Clear();
        attackController?.ResetSkillCooldown(CyberOrangeBossSO.CHARGE_ACTION_ID);
        attackController?.ResetSkillCooldown(CyberOrangeBossSO.BARRAGE_SKILL_ID);
        barrageShotsRemaining = 0;
        barrageWindupTimer = 0f;
        barrageShotTimer = 0f;
        chargeDirection = owner != null && owner.transform.localScale.x < 0f ? Vector2.left : Vector2.right;
        enrageApplied = false;
    }

    private void TryApplyEnrage()
    {
        if (enrageApplied || healthComponent == null || healthComponent.MaxHealth <= Mathf.Epsilon)
        {
            return;
        }

        float healthRatio = Mathf.Clamp01(healthComponent.CurrentHealth / healthComponent.MaxHealth);
        if (healthRatio > bossData.EnrageHealthRatio)
        {
            return;
        }

        enrageApplied = true;
        if (bossData.EnrageModifiers != null && bossData.EnrageModifiers.Count > 0)
        {
            AttributeManager.AddModifiers(ENRAGE_MODIFIER_SOURCE, bossData.EnrageModifiers);
        }

        if (bossData.EnrageSfxKey != AudioSfxKey.None)
        {
            AudioSfxBridge.RequestPlay(bossData.EnrageSfxKey);
        }
    }

    private bool IsChargeReady()
    {
        return target != null && attackController.CanUseSkill(CyberOrangeBossSO.CHARGE_ACTION_ID);
    }

    private bool IsBarrageReady()
    {
        return target != null &&
               attackController.CanUseSkill(CyberOrangeBossSO.BARRAGE_SKILL_ID) &&
               IsTargetInBarrageRange(target);
    }

    private void ApplyAttackMoveModifiers()
    {
        if (attackMoveModifiersApplied)
        {
            return;
        }

        AttributeManager.AddModifiers(ATTACK_MOVE_MODIFIER_SOURCE, bossData.AttackStateMoveModifiers);
        attackMoveModifiersApplied = true;
    }

    private void RemoveAttackMoveModifiers()
    {
        if (!attackMoveModifiersApplied)
        {
            return;
        }

        AttributeManager.RemoveModifiers(ATTACK_MOVE_MODIFIER_SOURCE);
        attackMoveModifiersApplied = false;
    }

    private void ApplyChargeModifiers()
    {
        if (chargeModifiersApplied)
        {
            return;
        }

        AttributeManager.AddModifiers(CHARGE_MODIFIER_SOURCE, bossData.ChargeModifiers);
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

    private void ClearRuntimeModifiers()
    {
        RemoveAttackMoveModifiers();
        RemoveChargeModifiers();
        AttributeManager?.RemoveModifiers(ENRAGE_MODIFIER_SOURCE);
    }

    private void CaptureChargeDirection()
    {
        Vector2 direction = target != null ? target.Center - owner.Center : currentMovable.MoveDirection;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            direction = ResolveFacingDirection();
        }

        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            direction = Vector2.right;
        }

        chargeDirection = direction.normalized;
    }

    private void CommitCharge()
    {
        ApplyChargeModifiers();
        chargeHitTargets.Clear();
        attackController.CommitSkillCooldown(CyberOrangeBossSO.CHARGE_ACTION_ID, bossData.ChargeCooldown);
        DealChargeDamage();
        if (bossData.ChargeScreenShake != null)
        {
            ScreenShakeBridge.Request(bossData.ChargeScreenShake, bossData.ChargeScreenShakeScale, owner.transform.position);
        }
    }

    private void DealChargeDamage()
    {
        if (bossData.ChargeDamageRadius <= 0f)
        {
            return;
        }

        int hitCount = Physics2D.OverlapCircleNonAlloc(owner.Center, bossData.ChargeDamageRadius, chargeHitBuffer, attackController.AttackLayer);
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
                AttributeManager.GetAttributeValue(PropType.Attack) * bossData.ChargeDamageMultiplier);
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

    private void BeginBarrage()
    {
        barrageShotsRemaining = bossData.BarrageShotCount;
        barrageWindupTimer = bossData.BarrageWindupDuration;
        barrageShotTimer = 0f;
        attackController.CommitSkillCooldown(CyberOrangeBossSO.BARRAGE_SKILL_ID, bossData.BarrageCooldown);
    }

    private bool TickBarrage()
    {
        if (barrageShotsRemaining <= 0)
        {
            return true;
        }

        if (barrageWindupTimer > 0f)
        {
            barrageWindupTimer = Mathf.Max(0f, barrageWindupTimer - Time.deltaTime);
            return false;
        }

        barrageShotTimer -= Time.deltaTime;
        if (barrageShotTimer > 0f)
        {
            return false;
        }

        FireBarrageShot();
        barrageShotsRemaining--;
        barrageShotTimer = bossData.BarrageShotInterval;
        return barrageShotsRemaining <= 0;
    }

    private void FireBarrageShot()
    {
        if (target == null || bossData.BarrageProjectileDefinition == null)
        {
            return;
        }

        Vector2 firePosition = shootPointTransform != null ? shootPointTransform.position : owner.Center;
        Vector2 targetPoint = target.GetClosestPointTo(firePosition);
        Vector2 direction = targetPoint - firePosition;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            direction = target.Center - owner.Center;
        }

        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            direction = ResolveFacingDirection();
        }

        direction.Normalize();
        float spread = ResolveShotSpreadOffset();
        Vector2 shotDirection = Quaternion.Euler(0f, 0f, spread) * direction;
        float damage = PropValueUtility.ClampNonNegative(
            AttributeManager.GetAttributeValue(PropType.Attack) * bossData.BarrageDamageMultiplier);
        Projectile projectile = ProjectileFactory.CreateProjectile(
            bossData.BarrageProjectileDefinition,
            firePosition,
            Quaternion.identity);
        attackController.LaunchProjectile(projectile, new ProjectileLaunchContext(
            attackController,
            owner,
            firePosition,
            shotDirection,
            HitSpec.EnemyHitSpec(damage),
            attackController.AttackLayer,
            bossData.BarrageProjectileDefinition,
            maxTravelDistance: ResolveAttackRangeWorldUnits(bossData.BarrageRangeMultiplier)));

        if (bossData.BarrageSfxKey != AudioSfxKey.None)
        {
            AudioSfxBridge.RequestPlay(bossData.BarrageSfxKey);
        }
    }

    private float ResolveShotSpreadOffset()
    {
        if (bossData.BarrageShotCount <= 1 || bossData.BarrageSpreadAngle <= 0f)
        {
            return 0f;
        }

        int firedIndex = bossData.BarrageShotCount - barrageShotsRemaining;
        float normalizedIndex = bossData.BarrageShotCount <= 1
            ? 0.5f
            : firedIndex / (float)(bossData.BarrageShotCount - 1);
        return Mathf.Lerp(-bossData.BarrageSpreadAngle * 0.5f, bossData.BarrageSpreadAngle * 0.5f, normalizedIndex);
    }

    private bool IsTargetInBarrageRange(Entity targetEntity)
    {
        if (targetEntity == null)
        {
            return false;
        }

        Vector2 firePosition = shootPointTransform != null ? shootPointTransform.position : owner.Center;
        Vector2 targetPoint = targetEntity.GetClosestPointTo(firePosition);
        float maxDistance = ResolveAttackRangeWorldUnits(bossData.BarrageRangeMultiplier);
        return (targetPoint - firePosition).sqrMagnitude <= maxDistance * maxDistance;
    }

    private BossState ResolveStateAfterCharge()
    {
        if (target == null)
        {
            return BossState.Idle;
        }

        if (meleeAttackStrategy.CanUse(target))
        {
            return BossState.Attack;
        }

        if (IsBarrageReady())
        {
            return BossState.Barrage;
        }

        return meleeAttackStrategy.IsTargetInRange(target)
            ? BossState.Idle
            : BossState.Chase;
    }

    private void RequestCombatState()
    {
        if (target == null)
        {
            stateMachine.RequestState(BossState.Idle);
            return;
        }

        if (IsChargeReady() && meleeAttackStrategy.IsTargetInRange(target))
        {
            stateMachine.RequestState(BossState.ChargeWindup);
            return;
        }

        if (meleeAttackStrategy.CanUse(target))
        {
            stateMachine.RequestState(BossState.Attack);
            return;
        }

        if (IsBarrageReady())
        {
            stateMachine.RequestState(BossState.Barrage);
            return;
        }

        stateMachine.RequestState(meleeAttackStrategy.IsTargetInRange(target) ? BossState.Idle : BossState.Chase);
    }

    private float ResolveAttackRangeWorldUnits(float multiplier)
    {
        return PropValueUtility.DistancePointsToEffectiveAttackRangeWorldUnits(
            AttributeManager.GetAttributeValue(PropType.AttackRange)) * Mathf.Max(0f, multiplier);
    }

    private Vector2 ResolveFacingDirection()
    {
        if (target != null)
        {
            Vector2 toTarget = target.Center - owner.Center;
            if (toTarget.sqrMagnitude > Mathf.Epsilon)
            {
                return toTarget.normalized;
            }
        }

        if (currentMovable != null && currentMovable.MoveDirection.sqrMagnitude > Mathf.Epsilon)
        {
            return currentMovable.MoveDirection.normalized;
        }

        return owner.transform.localScale.x < 0f ? Vector2.left : Vector2.right;
    }

    private Vector2 ResolveRangeDirection(Entity targetEntity)
    {
        if (targetEntity == null)
        {
            return ResolveFacingDirection();
        }

        Vector2 direction = targetEntity.Center - owner.Center;
        return direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : ResolveFacingDirection();
    }

    private sealed class IdleState : StateBase<BossState>
    {
        private readonly CyberOrangeBossBrain brain;

        public IdleState(CyberOrangeBossBrain brain) : base(BossState.Idle)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.currentMovable.StopMoving();
            brain.currentAnimatable.PlayState(brain.bossData.AnimConfig.IdleHash);
        }

        public override void OnUpdate()
        {
            brain.FaceTarget();
            brain.RequestCombatState();
        }
    }

    private sealed class ChaseState : StateBase<BossState>
    {
        private readonly CyberOrangeBossBrain brain;

        public ChaseState(CyberOrangeBossBrain brain) : base(BossState.Chase)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.currentAnimatable.PlayState(brain.bossData.AnimConfig.MoveHash);
        }

        public override void OnUpdate()
        {
            brain.FaceTarget();
            brain.RequestCombatState();
        }

        public override void OnFixedUpdate()
        {
            if (brain.target == null)
            {
                brain.currentMovable.StopMoving();
                return;
            }

            brain.chaseMoveStrategy.ExecuteMove(brain.target);
            brain.FaceTarget();
        }
    }

    private sealed class AttackState : EnemyActionStateBase<BossState>
    {
        private readonly CyberOrangeBossBrain brain;

        public AttackState(CyberOrangeBossBrain brain) : base(BossState.Attack)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.currentMovable.StopMoving();
            brain.ApplyAttackMoveModifiers();

            if (brain.target == null)
            {
                brain.stateMachine.RequestState(BossState.Idle, StateChangeMode.Force);
                return;
            }

            if (!brain.meleeAttackStrategy.CanUse(brain.target))
            {
                brain.stateMachine.RequestState(BossState.Chase, StateChangeMode.Force);
                return;
            }

            brain.FaceTarget();
            BeginAction(brain.bossData.AttackAction, brain.currentAnimatable);
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
            if (brain.meleeAttackStrategy.TryExecuteCommitted(brain.target) && brain.bossData.AttackScreenShake != null)
            {
                ScreenShakeBridge.Request(brain.bossData.AttackScreenShake, brain.bossData.AttackScreenShakeScale, brain.owner.transform.position);
            }
        }

        protected override void OnActionComplete()
        {
            brain.RequestCombatState();
        }
    }

    private sealed class ChargeWindupState : EnemyActionStateBase<BossState>
    {
        private readonly CyberOrangeBossBrain brain;

        public ChargeWindupState(CyberOrangeBossBrain brain) : base(BossState.ChargeWindup)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            if (brain.target == null || !brain.IsChargeReady())
            {
                brain.stateMachine.RequestState(BossState.Chase, StateChangeMode.Force);
                return;
            }

            brain.currentMovable.StopMoving();
            brain.CaptureChargeDirection();
            brain.facingController?.FaceDirection(brain.chargeDirection);
            BeginAction(brain.bossData.ChargeAction, brain.currentAnimatable);
        }

        public override void OnUpdate()
        {
            brain.facingController?.FaceDirection(brain.chargeDirection);
            TickAction(Time.deltaTime);
        }

        public override void OnFixedUpdate()
        {
            brain.currentMovable.StopMoving();
        }

        protected override void OnActionComplete()
        {
            brain.stateMachine.RequestState(BossState.Charge);
        }
    }

    private sealed class ChargeState : StateBase<BossState>
    {
        private readonly CyberOrangeBossBrain brain;
        private float elapsedTime;

        public ChargeState(CyberOrangeBossBrain brain) : base(BossState.Charge)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            elapsedTime = 0f;
            brain.CommitCharge();
            brain.facingController?.FaceDirection(brain.chargeDirection);
        }

        public override void OnUpdate()
        {
            elapsedTime += Time.deltaTime;
            if (elapsedTime >= brain.bossData.ChargeDuration)
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
        }
    }

    private sealed class BarrageState : StateBase<BossState>
    {
        private readonly CyberOrangeBossBrain brain;

        public BarrageState(CyberOrangeBossBrain brain) : base(BossState.Barrage)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            if (brain.target == null || !brain.IsBarrageReady())
            {
                brain.stateMachine.RequestState(BossState.Chase, StateChangeMode.Force);
                return;
            }

            brain.currentMovable.StopMoving();
            brain.FaceTarget();
            brain.currentAnimatable.PlayState(brain.bossData.AnimConfig.AttackHash);
            brain.BeginBarrage();
        }

        public override void OnUpdate()
        {
            brain.FaceTarget();
            if (brain.TickBarrage())
            {
                brain.RequestCombatState();
            }
        }

        public override void OnFixedUpdate()
        {
            brain.currentMovable.StopMoving();
        }
    }
}
