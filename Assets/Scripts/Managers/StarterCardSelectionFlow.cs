using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

public sealed class StarterCardSelectionFlow
{
    private const string POPUP_GROUP_ID = "starterCardSelection";

    private readonly UIManager uiManager;
    private readonly Player player;
    private readonly UnityEngine.Object logContext;
    private readonly UpgradeCardApplyService applyService = new();

    private StarterCardSelectionOption[] currentOptions = Array.Empty<StarterCardSelectionOption>();
    private UniTaskCompletionSource<RewardSelectionResult> selectionCompletionSource;

    public StarterCardSelectionFlow(UIManager uiManager, Player player, UnityEngine.Object logContext)
    {
        this.uiManager = uiManager != null
            ? uiManager
            : throw new ArgumentNullException(nameof(uiManager));
        this.player = player != null
            ? player
            : throw new ArgumentNullException(nameof(player));
        this.logContext = logContext;
    }

    public async UniTask RunAsync(IReadOnlyList<UpgradeCardSO> starterCards, CancellationToken cancellationToken)
    {
        currentOptions = CreateOptions(starterCards);
        if (currentOptions.Length == 0)
        {
            return;
        }

        RewardSelectionPopupModel model = new(
            "选择开局卡",
            "选择 1 张开局升级卡。",
            CreatePresentations(currentOptions),
            OnOptionSelected);
        PopupOptions popupOptions = new(
            closeOnOutsideClick: false,
            groupId: POPUP_GROUP_ID,
            replaceSameGroup: true,
            trackInStack: false,
            preferredAnchor: FloatingViewAnchor.Center);

        ViewHandle<RewardSelectionPopup> handle = await uiManager.ShowPopupAsync<RewardSelectionPopup>(
            model,
            popupOptions,
            cancellationToken);

        try
        {
            RewardSelectionResult result = await WaitForSelectionAsync(cancellationToken);
            if (!TryResolveSelectedOption(result, out StarterCardSelectionOption selectedOption))
            {
                Debug.LogWarning("[StarterCardSelectionFlow] Starter card selection result could not be resolved.", logContext);
                return;
            }

            if (!applyService.Apply(selectedOption.Card, player))
            {
                Debug.LogWarning($"[StarterCardSelectionFlow] Failed to apply starter card {selectedOption.Card?.name}.", logContext);
            }
        }
        finally
        {
            currentOptions = Array.Empty<StarterCardSelectionOption>();
            selectionCompletionSource = null;
            if (handle.IsValid)
            {
                await handle.CloseAsync(CloseReason.Normal);
            }
        }
    }

    private async UniTask<RewardSelectionResult> WaitForSelectionAsync(CancellationToken cancellationToken)
    {
        UniTaskCompletionSource<RewardSelectionResult> completionSource = new();
        selectionCompletionSource = completionSource;
        CancellationTokenRegistration registration =
            cancellationToken.Register(() => completionSource.TrySetCanceled());
        try
        {
            return await completionSource.Task;
        }
        finally
        {
            registration.Dispose();
            if (ReferenceEquals(selectionCompletionSource, completionSource))
            {
                selectionCompletionSource = null;
            }
        }
    }

    private void OnOptionSelected(int optionIndex, string optionId)
    {
        RewardSelectionResult result = new(optionIndex, optionId);
        if (!TryResolveSelectedOption(result, out _))
        {
            return;
        }

        selectionCompletionSource?.TrySetResult(result);
    }

    private bool TryResolveSelectedOption(
        RewardSelectionResult result,
        out StarterCardSelectionOption selectedOption)
    {
        selectedOption = default;
        if (result.OptionIndex < 0 || result.OptionIndex >= currentOptions.Length)
        {
            return false;
        }

        StarterCardSelectionOption candidate = currentOptions[result.OptionIndex];
        if (!string.Equals(candidate.OptionId, result.OptionId, StringComparison.Ordinal))
        {
            return false;
        }

        selectedOption = candidate;
        return true;
    }

    private static StarterCardSelectionOption[] CreateOptions(IReadOnlyList<UpgradeCardSO> starterCards)
    {
        if (starterCards == null || starterCards.Count == 0)
        {
            return Array.Empty<StarterCardSelectionOption>();
        }

        List<StarterCardSelectionOption> options = new();
        for (int i = 0; i < starterCards.Count; i++)
        {
            UpgradeCardSO card = starterCards[i];
            if (card == null)
            {
                continue;
            }

            options.Add(new StarterCardSelectionOption(card, CreatePresentation(card)));
        }

        return options.ToArray();
    }

    private static IRewardCardPresentation[] CreatePresentations(StarterCardSelectionOption[] options)
    {
        IRewardCardPresentation[] presentations = new IRewardCardPresentation[options.Length];
        for (int i = 0; i < options.Length; i++)
        {
            presentations[i] = options[i].Presentation;
        }

        return presentations;
    }

    private static UpgradeRewardCardPresentation CreatePresentation(UpgradeCardSO card)
    {
        return new UpgradeRewardCardPresentation(
            card.CardId,
            card.Title,
            card.Description,
            ContentTierResolver.FromUpgradeCardRarity(card.Rarity),
            BuildTagLabels(card.TagList),
            true);
    }

    private static string[] BuildTagLabels(IReadOnlyList<UpgradeCardTag> tags)
    {
        if (tags == null || tags.Count == 0)
        {
            return Array.Empty<string>();
        }

        string[] labels = new string[tags.Count];
        for (int i = 0; i < tags.Count; i++)
        {
            labels[i] = ItemDescriptionUtility.FormatUpgradeCardTag(tags[i]);
        }

        return labels;
    }

    private readonly struct StarterCardSelectionOption
    {
        public StarterCardSelectionOption(UpgradeCardSO card, IRewardCardPresentation presentation)
        {
            Card = card;
            Presentation = presentation;
        }

        public UpgradeCardSO Card { get; }
        public IRewardCardPresentation Presentation { get; }
        public string OptionId => Presentation != null ? Presentation.OptionId : string.Empty;
    }
}
