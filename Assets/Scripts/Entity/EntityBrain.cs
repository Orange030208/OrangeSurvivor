public abstract class EntityBrain : EntityComponentBase
{
    protected abstract void OnBrainStart();

    public abstract void StopBrain();

    public sealed override void Initialize(Entity owner)
    {
        OnInitialize(owner);
        OnBrainStart();
    }
    protected abstract void OnInitialize(Entity owner);
}