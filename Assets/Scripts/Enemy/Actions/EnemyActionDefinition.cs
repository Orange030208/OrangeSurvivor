using System;
using UnityEngine;

[Serializable]
public sealed class EnemyActionDefinition
{
    private const float DEFAULT_COMMIT_NORMALIZED_TIME = 0.5f;
    private const float DEFAULT_EXIT_NORMALIZED_TIME = 1f;
    private const float DEFAULT_DURATION = 0.1f;

    [SerializeField] private string actionId;
    [SerializeField] private string animationStateName;
    [SerializeField] private EnemyActionCompletionMode completionMode = EnemyActionCompletionMode.AnimationNormalizedTime;
    [SerializeField] private bool hasCommitPoint = true;
    [SerializeField, Range(0f, 1f)] private float commitNormalizedTime = DEFAULT_COMMIT_NORMALIZED_TIME;
    [SerializeField, Min(0f)] private float exitNormalizedTime = DEFAULT_EXIT_NORMALIZED_TIME;
    [SerializeField, Min(0f)] private float duration = DEFAULT_DURATION;
    [SerializeField] private bool stopMovementWhileRunning = true;
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
