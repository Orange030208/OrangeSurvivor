using UnityEngine;

public sealed class SkeletonMeteorhammer2Brain : SkeletonBrain
{
    private const int RECTANGLE_HIT_BUFFER_SIZE = 16;

    private readonly Collider2D[] rectangleHitBuffer = new Collider2D[RECTANGLE_HIT_BUFFER_SIZE];

    private SkeletonMeteorhammer2EnemySO meteorhammer2Data;
    private bool hasCommittedFirstAttack;
    private bool hasCommittedSecondAttack;

    protected override SkeletonEnemySO ResolveEnemyData()
    {
        meteorhammer2Data = owner.EnemyData as SkeletonMeteorhammer2EnemySO;
        return meteorhammer2Data;
    }

    protected override string RequiredEnemyDataTypeName => nameof(SkeletonMeteorhammer2EnemySO);

    protected override void ResetAttackRuntime()
    {
        hasCommittedFirstAttack = false;
        hasCommittedSecondAttack = false;
    }

    protected override void OnAttackActionCommit()
    {
        // 两段显式命中由动画进度分别提交，完整动作结束后统一提交普通攻击冷却。
    }

    protected override void OnAttackActionProgress(AnimationStateProgress progress)
    {
        if (!progress.IsPlaying || meteorhammer2Data == null)
        {
            return;
        }

        TryCommitFirstSegment(progress);
        TryCommitSecondSegment(progress);
    }

    protected override void OnAttackActionComplete()
    {
        CommitAttackCooldown();
    }

    private void TryCommitFirstSegment(AnimationStateProgress progress)
    {
        if (hasCommittedFirstAttack ||
            progress.NormalizedTime < meteorhammer2Data.FirstAttackCommitNormalizedTime)
        {
            return;
        }

        hasCommittedFirstAttack = true;
        ExecuteMeleeAreaAttack(
            meteorhammer2Data.FirstAttackRangeMultiplier,
            meteorhammer2Data.FirstAttackDamageMultiplier);
    }

    private void TryCommitSecondSegment(AnimationStateProgress progress)
    {
        if (hasCommittedSecondAttack ||
            progress.NormalizedTime < meteorhammer2Data.SecondAttackCommitNormalizedTime)
        {
            return;
        }

        hasCommittedSecondAttack = true;
        ExecuteForwardRectangleAttack();
    }

    private void ExecuteForwardRectangleAttack()
    {
        Vector2 attackPoint = ResolveMeleeAttackCenter();
        Vector2 attackDirection = ResolveLockedAttackDirection();
        float length = PropValueUtility.DistancePointsToEffectiveAttackRangeWorldUnits(
            owner.PropertiesManager.GetPropValue(PropType.AttackRange)) * meteorhammer2Data.SecondAttackLengthMultiplier;
        float width = meteorhammer2Data.SecondAttackWidth;
        float damage = ResolveDamage(meteorhammer2Data.SecondAttackDamageMultiplier);

        int hitCount = AreaHitQueryUtility.OverlapForwardBoxNonAlloc(
            attackPoint,
            length,
            width,
            attackDirection,
            rectangleHitBuffer,
            AttackController.AttackLayer);

        for (int i = 0; i < hitCount; i++)
        {
            Entity hitEntity = ResolveEntity(rectangleHitBuffer[i]);
            if (hitEntity == null || hitEntity == owner)
            {
                continue;
            }

            Vector2 hitPoint = hitEntity.GetClosestPointTo(attackPoint);
            Vector2 knockbackDirection = hitEntity.Center - owner.Center;
            HitService.Apply(new HitRequest(
                owner,
                hitEntity,
                HitSpec.EnemyHitSpec(damage),
                hitPoint,
                knockbackDirection,
                HitSourceKind.Direct,
                owner.Center));
        }
    }
}
