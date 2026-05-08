public sealed class StageCompletePageContext
{
    public StageCompletePageContext(StageCompleteSnapshot snapshot)
    {
        Snapshot = snapshot;
    }

    public StageCompleteSnapshot Snapshot { get; }
}
