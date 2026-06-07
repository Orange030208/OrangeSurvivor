using System;

public sealed class RewardSelectionPopupModel
{
    public RewardSelectionPopupModel(
        string title,
        string description,
        RewardCardViewConfig[] options,
        Action<int, string> optionSelected = null)
    {
        Title = title ?? string.Empty;
        Description = description ?? string.Empty;
        Options = options ?? Array.Empty<RewardCardViewConfig>();
        OptionSelected = optionSelected;
    }

    public string Title { get; }
    public string Description { get; }
    public RewardCardViewConfig[] Options { get; }
    public Action<int, string> OptionSelected { get; }
}

public readonly struct RewardSelectionCardBinding
{
    public RewardSelectionCardBinding(
        RewardCardViewConfig card,
        int index,
        Action<int, string> optionSelected)
        : this(card, index, optionSelected, null)
    {
    }

    public RewardSelectionCardBinding(
        RewardCardViewConfig card,
        int index,
        Action<int, string> optionSelected,
        Action<int, string> submitRequested)
    {
        Card = card;
        Index = index;
        OptionSelected = optionSelected;
        SubmitRequested = submitRequested;
    }

    public RewardCardViewConfig Card { get; }
    public int Index { get; }
    public Action<int, string> OptionSelected { get; }
    public Action<int, string> SubmitRequested { get; }
}
