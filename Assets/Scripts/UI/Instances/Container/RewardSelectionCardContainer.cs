using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

public class RewardSelectionCardContainer :
    UIContainerBase<RewardSelectionCardBinding,Describer>,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerMoveHandler
{
    [Header("卡片品质表现")]
    [SerializeField] private CardMotionController cardMotionController;
    [SerializeField] private CardQualityVisualController qualityVisualController;
    [SerializeField] private bool playRevealSfx = true;

    private CanvasGroup cardCanvasGroup;
    private CancellationTokenSource submitCancellation;
    private Func<int, bool> submitGate;
    private int containerIndex = -1;
    private bool isSubmitting;
    private bool interactionLocked;
    private bool isPointerPressed;
    private bool wasRaycastBlockingBeforeSubmit = true;

    public override void Configure(RewardSelectionCardBinding resource)
    {
        containerIndex = resource.Index;
        RewardSelectionCardViewModel option = resource.Card;
        iconImage.sprite = option.Icon;
        nameText.text = option.Title;
        bottom.Display(new RewardSelectionCardDisplayInfo(option, BuildDescription(option)));
        bool hasPresentationProfile = TryResolveQualityPresentationProfile(option.Quality, out CardQualityPresentationProfile presentationProfile);
        if (hasPresentationProfile)
        {
            ApplyQualityVisual(presentationProfile);
        }

        ConfigureCardMotionForReuse();
        if (hasPresentationProfile)
        {
            PlayRevealSfx(presentationProfile);
        }

        CleanClickEvent();
        OnClicked += _ =>
        {
            if (!option.Interactable)
            {
                return;
            }

            if (hasPresentationProfile)
            {
                PlaySelectSfx(presentationProfile);
            }

            resource.OptionSelected?.Invoke(resource.Index, option.OptionId);
        };
    }

    public void BindSubmitGate(Func<int, bool> submitGate)
    {
        this.submitGate = submitGate;
    }

    public void SetInteractionLocked(bool locked)
    {
        interactionLocked = locked;
        if (!locked)
        {
            return;
        }

        if (isPointerPressed)
        {
            isPointerPressed = false;
            GetCardMotionController()?.PlayRelease();
        }

        GetCardMotionController()?.PlayHoverOut();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        CardMotionController motionController = GetCardMotionController();
        if (isSubmitting || interactionLocked)
        {
            return;
        }

        motionController?.PlayHoverIn(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CardMotionController motionController = GetCardMotionController();
        if (isSubmitting || interactionLocked)
        {
            return;
        }

        if (isPointerPressed)
        {
            isPointerPressed = false;
            motionController?.PlayRelease();
        }

        motionController?.PlayHoverOut();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        CardMotionController motionController = GetCardMotionController();
        if (isSubmitting || interactionLocked || !CanReceivePointerInteraction(motionController))
        {
            return;
        }

        motionController?.UpdatePointerTilt(eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        CardMotionController motionController = GetCardMotionController();
        if (isSubmitting || interactionLocked || !CanReceivePointerInteraction(motionController) || !IsLeftButton(eventData))
        {
            return;
        }

        isPointerPressed = true;
        motionController?.PlayPress();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        CardMotionController motionController = GetCardMotionController();
        if (isSubmitting || interactionLocked || !CanReceivePointerInteraction(motionController) || !IsLeftButton(eventData))
        {
            return;
        }

        isPointerPressed = false;
        motionController?.PlayRelease();
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        CardMotionController motionController = GetCardMotionController();
        if (isSubmitting || interactionLocked || !CanReceivePointerInteraction(motionController) || !IsLeftButton(eventData))
        {
            return;
        }

        if (submitGate != null && !submitGate.Invoke(containerIndex))
        {
            return;
        }

        StopSubmitRoutine();
        submitCancellation = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        SubmitAfterClickMotionAsync(eventData, submitCancellation).Forget();
    }

    public override void Dispose()
    {
        StopSubmitRoutine();
        base.Dispose();
    }

    public async UniTask PlayRefreshOutAsync(CancellationToken cancellationToken)
    {
        CardMotionController motionController = GetCardMotionController();
        if (motionController == null)
        {
            return;
        }

        await motionController.PlayRefreshOutAsync(cancellationToken);
    }

    private void OnDisable()
    {
        StopSubmitRoutine();
    }

    private void ConfigureCardMotionForReuse()
    {
        GetCardMotionController()?.ConfigureForReuse();
    }

    private async UniTaskVoid SubmitAfterClickMotionAsync(PointerEventData eventData, CancellationTokenSource cancellationSource)
    {
        CancellationToken cancellationToken = cancellationSource.Token;
        bool shouldRaiseClicked = false;
        isSubmitting = true;
        isPointerPressed = false;
        SetRaycastBlocking(false);

        try
        {
            CardMotionController motionController = GetCardMotionController();
            if (motionController != null)
            {
                await motionController.PlaySelectAsync(cancellationToken);
                motionController.ResetToRest();
            }

            cancellationToken.ThrowIfCancellationRequested();
            shouldRaiseClicked = isActiveAndEnabled;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            SetRaycastBlocking(wasRaycastBlockingBeforeSubmit);
            isSubmitting = false;

            if (ReferenceEquals(submitCancellation, cancellationSource))
            {
                submitCancellation = null;
            }

            cancellationSource.Dispose();
        }

        if (shouldRaiseClicked)
        {
            RaiseClicked(eventData);
        }
    }

    private void StopSubmitRoutine()
    {
        if (submitCancellation != null)
        {
            CancellationTokenSource cancellationSource = submitCancellation;
            submitCancellation = null;
            cancellationSource.Cancel();
        }

        SetRaycastBlocking(wasRaycastBlockingBeforeSubmit);
        isSubmitting = false;
        interactionLocked = false;
        isPointerPressed = false;
        GetCardMotionController()?.CancelAndReset();
    }

    private CardMotionController GetCardMotionController()
    {
        if (cardMotionController == null)
        {
            cardMotionController = GetComponent<CardMotionController>();
        }

        return cardMotionController;
    }

    private void ApplyQualityVisual(CardQualityPresentationProfile presentationProfile)
    {
        if (qualityVisualController == null)
        {
            qualityVisualController = GetComponent<CardQualityVisualController>();
        }

        qualityVisualController?.Apply(presentationProfile);
    }

    private void SetRaycastBlocking(bool blocksRaycasts)
    {
        if (cardCanvasGroup == null)
        {
            cardCanvasGroup = GetComponent<CanvasGroup>();
        }

        if (cardCanvasGroup == null)
        {
            return;
        }

        if (!blocksRaycasts)
        {
            wasRaycastBlockingBeforeSubmit = cardCanvasGroup.blocksRaycasts;
        }

        cardCanvasGroup.blocksRaycasts = blocksRaycasts;
    }

    private static bool IsLeftButton(PointerEventData eventData)
    {
        return eventData == null || eventData.button == PointerEventData.InputButton.Left;
    }

    private static bool CanReceivePointerInteraction(CardMotionController motionController)
    {
        return motionController == null || motionController.CanReceiveInteraction;
    }

    private static bool TryResolveQualityPresentationProfile(CardQuality quality, out CardQualityPresentationProfile profile)
    {
        CardQualityPresentationCatalogSO catalog = GameContentRuntime.TryGetProvider(out IGameContentProvider provider)
            ? provider.CardQualityPresentationCatalog
            : null;
        if (catalog == null)
        {
            profile = default;
            Debug.LogError($"{nameof(RewardSelectionCardContainer)} could not load Card Quality Presentation Catalog.");
            return false;
        }

        if (!catalog.TryGetProfile(quality, out profile))
        {
            Debug.LogError($"{nameof(RewardSelectionCardContainer)} could not find card quality presentation profile '{quality}'.");
            return false;
        }

        return true;
    }

    private void PlayRevealSfx(CardQualityPresentationProfile profile)
    {
        if (!playRevealSfx || profile.RevealSfxKey == AudioSfxKey.None)
        {
            return;
        }

        AudioSfxBridge.RequestPlay(profile.RevealSfxKey);
    }

    private static void PlaySelectSfx(CardQualityPresentationProfile profile)
    {
        if (profile.SelectSfxKey == AudioSfxKey.None)
        {
            return;
        }

        AudioSfxBridge.RequestPlay(profile.SelectSfxKey);
    }

    private static string BuildDescription(RewardSelectionCardViewModel option)
    {
        string description = option.Description;
        string tagText = BuildTagText(option.Tags);
        return $"{GetQualityText(option.Quality)}{tagText}\n{description}";
    }

    private sealed class RewardSelectionCardDisplayInfo : IDescribable
    {
        public RewardSelectionCardDisplayInfo(RewardSelectionCardViewModel option, string description)
        {
            Title = option.Title;
            Icon = option.Icon;
            Description = description;
        }

        public string Title { get; }
        public Sprite Icon { get; }
        public string Description { get; }

        public IEnumerable<DescriptorInfo> GetExtraInfos()
        {
            if (string.IsNullOrWhiteSpace(Description))
            {
                yield break;
            }

            yield return new DescriptorInfo(string.Empty, Description);
        }
    }

    private static string GetQualityText(CardQuality quality)
    {
        return quality switch
        {
            CardQuality.Rare => "稀有",
            CardQuality.Epic => "史诗",
            CardQuality.Legendary => "传说",
            _ => "普通"
        };
    }

    private static string BuildTagText(string[] tags)
    {
        if (tags == null || tags.Length == 0)
        {
            return string.Empty;
        }

        int count = Mathf.Min(2, tags.Length);
        string result = " · ";
        for (int i = 0; i < count; i++)
        {
            if (i > 0)
            {
                result += "/";
            }

            result += tags[i];
        }

        return result;
    }
}
