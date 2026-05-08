public sealed class StageCompletePageContext
{
    public StageCompletePageContext(StageCompleteResult result)
    {
        Result = result;
    }

    public StageCompleteResult Result { get; }
}
