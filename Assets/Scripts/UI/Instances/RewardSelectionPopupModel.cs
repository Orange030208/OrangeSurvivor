using System;

public sealed class RewardSelectionPopupModel
{
    public RewardSelectionPopupModel(
        string title,
        string description,
        IRewardCardPresentation[] options,
        Action<int, string> optionSelected = null)
    {
        Title = title ?? string.Empty;
        Description = description ?? string.Empty;
        Options = options ?? Array.Empty<IRewardCardPresentation>();
        OptionSelected = optionSelected;
    }

    public string Title { get; }
    public string Description { get; }
    public IRewardCardPresentation[] Options { get; }
    public Action<int, string> OptionSelected { get; }
}

public readonly struct RewardSelectionCardBinding
{
    public RewardSelectionCardBinding(
        IRewardCardPresentation card,
        int index,
        Action<int, string> optionSelected)
    {
        Card = card;
        Index = index;
        OptionSelected = optionSelected;
    }

    public IRewardCardPresentation Card { get; }
    public int Index { get; }
    public Action<int, string> OptionSelected { get; }
}
