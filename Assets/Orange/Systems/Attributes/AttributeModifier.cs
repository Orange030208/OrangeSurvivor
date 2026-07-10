namespace Orange.Attributes
{
    public readonly struct AttributeModifier<TKey>
    where TKey : notnull
    {
        public readonly TKey AttributeId;
        public readonly AttributeModifierType ModifierType;
        public readonly int Value;

        public AttributeModifier(TKey attributeId, AttributeModifierType modifierType, int value)
        {
            AttributeId = attributeId;
            ModifierType = modifierType;
            Value = value;
        }
    }
}
