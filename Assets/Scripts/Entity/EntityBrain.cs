public abstract class EntityBrain : EntityComponentBase
{
    private bool brainStartInvoked;

    protected abstract void OnBrainStart();

    public abstract void StopBrain();

    public abstract void StartBrain();

    protected bool HasBrainStarted => brainStartInvoked;

    protected virtual bool ShouldStartOnInitialize => true;

    public sealed override void Initialize(Entity owner)
    {
        OnInitialize(owner);
        if (ShouldStartOnInitialize)
        {
            EnsureBrainStarted();
        }
    }

    protected void EnsureBrainStarted()
    {
        if (brainStartInvoked)
        {
            return;
        }

        brainStartInvoked = true;
        OnBrainStart();
    }

    protected abstract void OnInitialize(Entity owner);
}
