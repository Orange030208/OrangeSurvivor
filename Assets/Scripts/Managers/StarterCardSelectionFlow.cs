using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

public sealed class StarterCardSelectionFlow
{
    private const string POPUP_GROUP_ID = "starterCardSelection";

    private readonly Player player;
    private readonly UnityEngine.Object logContext;
    private readonly RewardCardApplyService applyService = new();

    private StarterCardSelectionOption[] currentOptions = Array.Empty<StarterCardSelectionOption>();
    private UniTaskCompletionSource<RewardSelectionResult> selectionCompletionSource;

    public StarterCardSelectionFlow(Player player, UnityEngine.Object logContext)
    {
        this.player = player != null
            ? player
            : throw new ArgumentNullException(nameof(player));
        this.logContext = logContext;
    }

    public async UniTask RunAsync(IReadOnlyList<RewardCardSO> starterCards, CancellationToken cancellationToken)
    {
        currentOptions = CreateOptions(starterCards);
        if (currentOptions.Length == 0)
        {
            return;
        }

        RewardSelectionPopupModel model = new(
            "选择开局卡",
            "选择 1 张开局升级卡。",
            CreateViewConfigs(currentOptions),
            OnOptionSelected);
        PopupOptions popupOptions = new(
            closeOnOutsideClick: false,
            groupId: POPUP_GROUP_ID,
            replaceSameGroup: true,
            trackInStack: false,
            preferredAnchor: FloatingViewAnchor.Center,
            showBackdrop: true);

        ViewHandle<RewardSelectionPopup> handle = await UIManager.Instance.ShowPopupAsync<RewardSelectionPopup>(
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

    private static StarterCardSelectionOption[] CreateOptions(IReadOnlyList<RewardCardSO> starterCards)
    {
        if (starterCards == null || starterCards.Count == 0)
        {
            return Array.Empty<StarterCardSelectionOption>();
        }

        List<StarterCardSelectionOption> options = new();
        for (int i = 0; i < starterCards.Count; i++)
        {
            RewardCardSO card = starterCards[i];
            if (card == null)
            {
                continue;
            }

            options.Add(new StarterCardSelectionOption(card, RewardCardViewConfigFactory.CreateUpgrade(card)));
        }

        return options.ToArray();
    }

    private static RewardCardViewConfig[] CreateViewConfigs(StarterCardSelectionOption[] options)
    {
        RewardCardViewConfig[] viewConfigs = new RewardCardViewConfig[options.Length];
        for (int i = 0; i < options.Length; i++)
        {
            viewConfigs[i] = options[i].ViewConfig;
        }

        return viewConfigs;
    }

    private readonly struct StarterCardSelectionOption
    {
        public StarterCardSelectionOption(RewardCardSO card, RewardCardViewConfig viewConfig)
        {
            Card = card;
            ViewConfig = viewConfig;
        }

        public RewardCardSO Card { get; }
        public RewardCardViewConfig ViewConfig { get; }
        public string OptionId => ViewConfig != null ? ViewConfig.OptionId : string.Empty;
    }
}
