public readonly struct RewardSelectionResult
{
    public int OptionIndex { get; }
    public string OptionId { get; }

    public RewardSelectionResult(int optionIndex, string optionId)
    {
        OptionIndex = optionIndex;
        OptionId = optionId ?? string.Empty;
    }
}
