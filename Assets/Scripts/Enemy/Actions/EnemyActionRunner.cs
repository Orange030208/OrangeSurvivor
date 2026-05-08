using System;
using UnityEngine;

public sealed class EnemyActionRunner
{
    private EnemyActionDefinition definition;
    private IAnimatable animatable;
    private float elapsedTime;
    private bool isRunning;
    private bool isComplete;
    private bool isCommitted;

    public bool IsRunning => isRunning;
    public bool IsComplete => isComplete;
    public bool IsCommitted => isCommitted;
    public EnemyActionDefinition Definition => definition;
    public float ElapsedTime => elapsedTime;
    public AnimationStateProgress Progress { get; private set; }

    public bool ShouldCommit
    {
        get
        {
            return (isRunning || isComplete) &&
                   !isCommitted &&
                   definition != null &&
                   definition.HasCommitPoint &&
                   Progress.IsPlaying &&
                   Progress.NormalizedTime >= definition.CommitNormalizedTime;
        }
    }

    public void Begin(EnemyActionDefinition definition, IAnimatable animatable)
    {
        this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
        this.animatable = animatable ?? throw new ArgumentNullException(nameof(animatable));

        elapsedTime = 0f;
        isRunning = true;
        isComplete = false;
        isCommitted = !definition.HasCommitPoint;
        Progress = default;

        if (definition.AnimationStateHash != 0)
        {
            this.animatable.PlayState(definition.AnimationStateHash, 0f);
        }
    }

    public void Tick(float deltaTime)
    {
        if (!isRunning || isComplete || definition == null)
        {
            return;
        }

        elapsedTime += Mathf.Max(0f, deltaTime);
        Progress = ResolveProgress();

        switch (definition.CompletionMode)
        {
            case EnemyActionCompletionMode.AnimationNormalizedTime:
                if (Progress.IsComplete(definition.ExitNormalizedTime))
                {
                    MarkComplete();
                }
                break;
            case EnemyActionCompletionMode.Duration:
                if (elapsedTime >= definition.Duration)
                {
                    MarkComplete();
                }
                break;
            case EnemyActionCompletionMode.Manual:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void MarkCommitted()
    {
        isCommitted = true;
    }

    public void MarkComplete()
    {
        if (definition != null && definition.HasCommitPoint && !isCommitted)
        {
            isCommitted = true;
        }

        isComplete = true;
        isRunning = false;
    }

    public void Cancel()
    {
        definition = null;
        animatable = null;
        elapsedTime = 0f;
        isRunning = false;
        isComplete = false;
        isCommitted = false;
        Progress = default;
    }

    private AnimationStateProgress ResolveProgress()
    {
        if (definition == null || animatable == null || definition.AnimationStateHash == 0)
        {
            return new AnimationStateProgress(false, 0f);
        }

        return animatable.GetStateProgress(definition.AnimationStateHash);
    }
}
