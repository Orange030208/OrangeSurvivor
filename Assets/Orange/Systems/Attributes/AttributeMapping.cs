namespace Orange.Attributes
{
    public readonly struct AttributeMapping<TKey>
    where TKey : notnull
    {
        public readonly TKey SourceAttributeId;
        public readonly TKey TargetAttributeId;
        public readonly int ConversionRatio;

        public AttributeMapping(TKey sourceAttributeId, TKey targetAttributeId, int conversionRatio)
        {
            SourceAttributeId = sourceAttributeId;
            TargetAttributeId = targetAttributeId;
            ConversionRatio = conversionRatio;
        }
    }
}
