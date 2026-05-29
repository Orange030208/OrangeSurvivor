using System;

public sealed class UpgradeRewardCardPresenter
{
    public UpgradeRewardCardPresentation Create(UpgradeCardRollOption rollOption)
    {
        UpgradeCardSO card = rollOption.Card;
        UpgradeCardOptionViewData viewData = rollOption.CreateViewData();
        return new UpgradeRewardCardPresentation(
            viewData.CardId,
            viewData.Title,
            viewData.Description,
            ContentTierResolver.FromUpgradeCardRarity(viewData.Rarity),
            BuildTagLabels(viewData.Tags),
            card != null);
    }

    private static string[] BuildTagLabels(UpgradeCardTag[] tags)
    {
        if (tags == null || tags.Length == 0)
        {
            return Array.Empty<string>();
        }

        string[] labels = new string[tags.Length];
        for (int i = 0; i < tags.Length; i++)
        {
            labels[i] = ItemDescriptionUtility.FormatUpgradeCardTag(tags[i]);
        }

        return labels;
    }
}
