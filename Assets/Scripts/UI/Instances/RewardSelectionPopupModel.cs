using System;
using UnityEngine;

public sealed class RewardSelectionPopupModel
{
    public RewardSelectionPopupModel(
        string title,
        string description,
        RewardSelectionCardViewModel[] options,
        Action<int, string> optionSelected = null)
    {
        Title = title ?? string.Empty;
        Description = description ?? string.Empty;
        Options = options ?? Array.Empty<RewardSelectionCardViewModel>();
        OptionSelected = optionSelected;
    }

    public string Title { get; }
    public string Description { get; }
    public RewardSelectionCardViewModel[] Options { get; }
    public Action<int, string> OptionSelected { get; }
}

public readonly struct RewardSelectionCardViewModel
{
    public RewardSelectionCardViewModel(
        string optionId,
        string title,
        Sprite icon,
        string description,
        CardQuality quality,
        string[] tags,
        bool interactable = true)
    {
        OptionId = optionId ?? string.Empty;
        Title = title ?? string.Empty;
        Icon = icon;
        Description = description ?? string.Empty;
        Quality = quality;
        Tags = tags ?? Array.Empty<string>();
        Interactable = interactable;
    }

    public string OptionId { get; }
    public string Title { get; }
    public Sprite Icon { get; }
    public string Description { get; }
    public CardQuality Quality { get; }
    public string[] Tags { get; }
    public bool Interactable { get; }
}

public readonly struct RewardSelectionCardBinding
{
    public RewardSelectionCardBinding(
        RewardSelectionCardViewModel card,
        int index,
        Action<int, string> optionSelected)
    {
        Card = card;
        Index = index;
        OptionSelected = optionSelected;
    }

    public RewardSelectionCardViewModel Card { get; }
    public int Index { get; }
    public Action<int, string> OptionSelected { get; }
}
