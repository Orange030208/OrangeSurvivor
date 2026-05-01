public enum UIMotionConflictPolicy
{
    // 停止同 Channel 上的旧动画。大多数 UI 动作应使用这个默认策略。
    StopSameChannel,
    // 停止当前 Player 上所有动画。适合页面级切换或需要强制收束的状态。
    StopAllChannels,
    // 不停止旧动画。仅在确认多个 Tween 不会争抢同一属性时使用。
    AllowParallel
}
