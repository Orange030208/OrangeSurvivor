namespace Orange.Extraction
{
    /// <summary>
    /// Returns the final effective weight for one entry under a business-defined context.
    /// </summary>
    public interface IExtractionWeightModifier<TItem, TContext>
    {
        float ModifyWeight(
            WeightedExtractionEntry<TItem, TContext> entry,
            float baseWeight,
            TContext context);
    }
}
