using System;

public interface IRewardSelectionHandler
{
    RewardSelectionReason Reason { get; }
    bool ShouldCreateSelection(RewardSelectionHandlerContext context, bool hasProcessedSelection);
    RewardSelectionRound CreateSelection(RewardSelectionHandlerContext context);
    bool ApplySelection(RewardSelectionOption option, RewardSelectionHandlerContext context);
}

public sealed class RewardSelectionRound
{
    public RewardSelectionRound(string title, string description, RewardSelectionOption[] options)
    {
        Title = title ?? string.Empty;
        Description = description ?? string.Empty;
        Options = options ?? Array.Empty<RewardSelectionOption>();
    }

    public string Title { get; }
    public string Description { get; }
    public RewardSelectionOption[] Options { get; }
    public bool HasAnyOption => Options.Length > 0;

    public IRewardCardPresentation[] CreatePresentations()
    {
        IRewardCardPresentation[] presentations = new IRewardCardPresentation[Options.Length];
        for (int i = 0; i < Options.Length; i++)
        {
            presentations[i] = Options[i]?.Presentation;
        }

        return presentations;
    }
}
