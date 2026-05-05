public sealed class StageCompletePageContext : IPageContext
{
    public StageCompletePageContext(StageCompleteSnapshot snapshot)
    {
        Snapshot = snapshot;
    }

    public StageCompleteSnapshot Snapshot { get; }

    public void Dispose()
    {
    }
}
