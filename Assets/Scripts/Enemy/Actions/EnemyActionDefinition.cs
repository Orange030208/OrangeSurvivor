using System;
using UnityEngine;

[Serializable]
public sealed class EnemyActionDefinition
{
    private const float DEFAULT_COMMIT_NORMALIZED_TIME = 0.5f;
    private const float DEFAULT_EXIT_NORMALIZED_TIME = 1f;
    private const float DEFAULT_DURATION = 0.1f;

    [Tooltip("动作唯一标识，用于行为代码识别当前动作。")]
    [SerializeField] private string actionId;
    [Tooltip("进入动作时播放的 Animator 状态名；留空则不主动切换动画状态。")]
    [SerializeField] private string animationStateName;
    [Tooltip("动作完成方式：按动画归一化时间结束、按固定秒数结束，或由行为代码手动结束。")]
    [SerializeField] private EnemyActionCompletionMode completionMode = EnemyActionCompletionMode.AnimationNormalizedTime;
    [Tooltip("是否存在动作提交点。开启后，到达提交点时会触发伤害、投射物、技能效果等一次性逻辑。")]
    [SerializeField] private bool hasCommitPoint = true;
    [Tooltip("动作提交点的动画归一化时间，0 表示动画开始，1 表示动画结束；只有开启提交点且动画状态正在播放时生效。")]
    [SerializeField, Range(0f, 1f)] private float commitNormalizedTime = DEFAULT_COMMIT_NORMALIZED_TIME;
    [Tooltip("按动画归一化时间完成时的退出点。动画进度达到该值后动作结束；例如 1 表示播完一轮动画。")]
    [SerializeField, Min(0f)] private float exitNormalizedTime = DEFAULT_EXIT_NORMALIZED_TIME;
    [Tooltip("按固定时长完成时的动作持续秒数。只有 Completion Mode 设为 Duration 时才用于判定动作结束；动画归一化时间模式和手动模式不会用它结束动作。")]
    [SerializeField, Min(0f)] private float duration = DEFAULT_DURATION;
    [Tooltip("动作运行期间是否停止敌人移动。具体移动控制由使用该动作的状态或 Brain 执行。")]
    [SerializeField] private bool stopMovementWhileRunning = true;
    [Tooltip("动作运行期间允许被哪些状态切换打断。Force Only 表示只能被强制切换中断。")]
    [SerializeField] private EnemyActionInterruptPolicy interruptPolicy = EnemyActionInterruptPolicy.ForceOnly;

    [NonSerialized] private int animationStateHash;
    [NonSerialized] private string cachedAnimationStateName;

    public string ActionId => actionId;
    public string AnimationStateName => animationStateName;
    public EnemyActionCompletionMode CompletionMode => completionMode;
    public bool HasCommitPoint => hasCommitPoint;
    public float CommitNormalizedTime => commitNormalizedTime;
    public float ExitNormalizedTime => Mathf.Max(0f, exitNormalizedTime);
    public float Duration => Mathf.Max(0f, duration);
    public bool StopMovementWhileRunning => stopMovementWhileRunning;
    public EnemyActionInterruptPolicy InterruptPolicy => interruptPolicy;
    public int AnimationStateHash
    {
        get
        {
            if (!string.Equals(cachedAnimationStateName, animationStateName, StringComparison.Ordinal))
            {
                cachedAnimationStateName = animationStateName;
                animationStateHash = string.IsNullOrWhiteSpace(animationStateName)
                    ? 0
                    : Animator.StringToHash(animationStateName);
            }

            return animationStateHash;
        }
    }

    public EnemyActionDefinition()
    {
    }

    public EnemyActionDefinition(
        string actionId,
        string animationStateName,
        float commitNormalizedTime = DEFAULT_COMMIT_NORMALIZED_TIME,
        EnemyActionCompletionMode completionMode = EnemyActionCompletionMode.AnimationNormalizedTime)
    {
        this.actionId = actionId;
        this.animationStateName = animationStateName;
        this.commitNormalizedTime = Mathf.Clamp01(commitNormalizedTime);
        this.completionMode = completionMode;
    }

    public void ConfigureDefaults(
        string defaultActionId,
        string defaultAnimationStateName,
        float defaultCommitNormalizedTime,
        EnemyActionCompletionMode defaultCompletionMode = EnemyActionCompletionMode.AnimationNormalizedTime,
        bool defaultHasCommitPoint = true,
        float defaultDuration = DEFAULT_DURATION)
    {
        bool isUninitialized = string.IsNullOrWhiteSpace(actionId) && string.IsNullOrWhiteSpace(animationStateName);
        if (string.IsNullOrWhiteSpace(actionId))
        {
            actionId = defaultActionId;
        }

        if (string.IsNullOrWhiteSpace(animationStateName))
        {
            animationStateName = defaultAnimationStateName;
        }

        if (isUninitialized)
        {
            completionMode = defaultCompletionMode;
            hasCommitPoint = defaultHasCommitPoint;
            commitNormalizedTime = Mathf.Clamp01(defaultCommitNormalizedTime);
            if (defaultCompletionMode == EnemyActionCompletionMode.Duration)
            {
                duration = Mathf.Max(DEFAULT_DURATION, defaultDuration);
            }
        }

        if (completionMode == EnemyActionCompletionMode.Duration && (duration <= 0f || isUninitialized))
        {
            duration = Mathf.Max(DEFAULT_DURATION, defaultDuration);
        }

        Validate();
    }

    public void Validate()
    {
        commitNormalizedTime = Mathf.Clamp01(commitNormalizedTime);
        exitNormalizedTime = Mathf.Max(0f, exitNormalizedTime);
        duration = Mathf.Max(0f, duration);

        if (hasCommitPoint)
        {
            commitNormalizedTime = Mathf.Min(commitNormalizedTime, exitNormalizedTime);
        }

        if (completionMode == EnemyActionCompletionMode.AnimationNormalizedTime && exitNormalizedTime <= 0f)
        {
            exitNormalizedTime = DEFAULT_EXIT_NORMALIZED_TIME;
        }

        if (completionMode == EnemyActionCompletionMode.Duration && duration <= 0f)
        {
            duration = DEFAULT_DURATION;
        }

        cachedAnimationStateName = null;
    }
}
