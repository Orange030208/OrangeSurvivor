public sealed class ContentPoolEvaluationContext
{
    public ContentPoolEvaluationContext(
        ContentPoolPurpose purpose,
        ContentFactSet facts,
        ContentPoolRuntimeState runtimeState)
    {
        Purpose = purpose;
        Facts = facts ?? ContentFactSet.Empty;
        RuntimeState = runtimeState;
    }

    public ContentPoolPurpose Purpose { get; }
    public ContentFactSet Facts { get; }
    public ContentPoolRuntimeState RuntimeState { get; }
}
