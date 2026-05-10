public sealed class SkeletonMeteorhammerBrain : SkeletonBrain
{
    private SkeletonMeteorhammerEnemySO meteorhammerData;
    private bool hasCommittedFirstAttack;
    private bool hasCommittedSecondAttack;

    protected override SkeletonEnemySO ResolveEnemyData()
    {
        meteorhammerData = owner.EnemyData as SkeletonMeteorhammerEnemySO;
        return meteorhammerData;
    }

    protected override string RequiredEnemyDataTypeName => nameof(SkeletonMeteorhammerEnemySO);

    protected override void ResetAttackRuntime()
    {
        hasCommittedFirstAttack = false;
        hasCommittedSecondAttack = false;
    }

    protected override void OnAttackActionCommit()
    {
        // 流星锤使用两段显式攻击时机，并在完整动作结束后提交冷却。
    }

    protected override void OnAttackActionProgress(AnimationStateProgress progress)
    {
        if (!progress.IsPlaying || meteorhammerData == null)
        {
            return;
        }

        TryCommitSegment(
            ref hasCommittedFirstAttack,
            meteorhammerData.FirstAttackCommitNormalizedTime,
            meteorhammerData.FirstAttackRangeMultiplier,
            progress);
        TryCommitSegment(
            ref hasCommittedSecondAttack,
            meteorhammerData.SecondAttackCommitNormalizedTime,
            meteorhammerData.SecondAttackRangeMultiplier,
            progress);
    }

    protected override void OnAttackActionComplete()
    {
        CommitAttackCooldown();
    }

    private void TryCommitSegment(
        ref bool hasCommitted,
        float commitNormalizedTime,
        float rangeMultiplier,
        AnimationStateProgress progress)
    {
        if (hasCommitted || progress.NormalizedTime < commitNormalizedTime)
        {
            return;
        }

        hasCommitted = true;
        ExecuteMeleeAreaAttack(rangeMultiplier);
    }
}
