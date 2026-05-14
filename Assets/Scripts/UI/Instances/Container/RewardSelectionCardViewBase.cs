using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class RewardSelectionCardViewBase :
    ViewPartBase,
    IDisposable,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerMoveHandler
{
    [SerializeField] protected TextMeshProUGUI nameText;
    [SerializeField] protected Describer bottom;

    [Header("卡片品质表现")]
    [SerializeField] private CardQualityVisualController qualityVisual;
    [SerializeField] private CardQualityPresentationCatalogSO qualityPresentationCatalogOverride;

    [Header("卡片动效")]
    [SerializeField] private CardMotionController motion;
    [SerializeField] private bool playRevealSfx = true;

    private CanvasGroup cardCanvasGroup;
    private CancellationTokenSource submitCancellation;
    private Func<int, bool> submitGate;
    private int containerIndex = -1;
    private bool isSubmitting;
    private bool interactionLocked;
    private bool isPointerPressed;
    private bool currentOptionInteractable;
    private bool wasRaycastBlockingBeforeSubmit = true;

    protected abstract RewardOptionKind ExpectedKind { get; }
    protected virtual string ExpectedKindDescription => ExpectedKind.ToString();

    protected virtual bool SupportsKind(RewardOptionKind kind)
    {
        return kind == ExpectedKind;
    }

    private event Action<PointerEventData> OnClicked;

    public void Configure(RewardSelectionCardBinding resource)
    {
        containerIndex = resource.Index;
        IRewardCardPresentation option = resource.Card;
        if (option == null)
        {
            Debug.LogError($"{GetType().Name} '{name}' received a null reward presentation.", this);
            return;
        }

        ValidatePresentationKind(option);
        currentOptionInteractable = option.Interactable;
        RenderPresentation(option);

        CardQualityPresentationProfile presentationProfile = default;
        bool hasPresentationProfile = TryResolveQualityPresentationProfile(
            option.Quality,
            out presentationProfile);
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
            if (!currentOptionInteractable)
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
            GetMotion()?.PlayRelease();
        }

        GetMotion()?.PlayHoverOut();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        CardMotionController motionController = GetMotion();
        if (isSubmitting || interactionLocked)
        {
            return;
        }

        motionController?.PlayHoverIn(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CardMotionController motionController = GetMotion();
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
        CardMotionController motionController = GetMotion();
        if (isSubmitting || interactionLocked || !CanReceivePointerInteraction(motionController))
        {
            return;
        }

        motionController?.UpdatePointerTilt(eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        CardMotionController motionController = GetMotion();
        if (!currentOptionInteractable ||
            isSubmitting ||
            interactionLocked ||
            !CanReceivePointerInteraction(motionController) ||
            !IsLeftButton(eventData))
        {
            return;
        }

        isPointerPressed = true;
        motionController?.PlayPress();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        CardMotionController motionController = GetMotion();
        if (isSubmitting || interactionLocked || !CanReceivePointerInteraction(motionController) || !IsLeftButton(eventData))
        {
            return;
        }

        isPointerPressed = false;
        motionController?.PlayRelease();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        CardMotionController motionController = GetMotion();
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

    public void Dispose()
    {
        StopSubmitRoutine();
        CleanClickEvent();
    }

    public async UniTask PlayRefreshOutAsync(CancellationToken cancellationToken)
    {
        CardMotionController motionController = GetMotion();
        if (motionController == null)
        {
            return;
        }

        await motionController.PlayRefreshOutAsync(cancellationToken);
    }

    protected virtual void RenderPresentation(IRewardCardPresentation option)
    {
        if (nameText != null)
        {
            nameText.text = option.Title;
        }

        if (bottom != null)
        {
            bottom.Display(option);
        }
    }

    private void CleanClickEvent()
    {
        OnClicked = null;
    }

    private void RaiseClicked(PointerEventData eventData)
    {
        OnClicked?.Invoke(eventData);
    }

    private void OnDisable()
    {
        StopSubmitRoutine();
    }

    private void OnDestroy()
    {
        Dispose();
    }

    private void ConfigureCardMotionForReuse()
    {
        GetMotion()?.ConfigureForReuse();
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
            CardMotionController motionController = GetMotion();
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
        GetMotion()?.CancelAndReset();
    }

    private CardMotionController GetMotion()
    {
        if (motion == null)
        {
            motion = GetComponent<CardMotionController>();
        }

        return motion;
    }

    private void ApplyQualityVisual(CardQualityPresentationProfile presentationProfile)
    {
        if (qualityVisual == null)
        {
            qualityVisual = GetComponent<CardQualityVisualController>();
        }

        qualityVisual?.Apply(presentationProfile);
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

    private void ValidatePresentationKind(IRewardCardPresentation option)
    {
        if (SupportsKind(option.Kind))
        {
            return;
        }

        throw new ArgumentException(
            $"{GetType().Name} '{name}' expects reward kind '{ExpectedKindDescription}' but received '{option.Kind}'.",
            nameof(option));
    }

    private bool TryResolveQualityPresentationProfile(CardQuality quality, out CardQualityPresentationProfile profile)
    {
        CardQualityPresentationCatalogSO catalog = ResolveQualityPresentationCatalog();
        if (catalog == null)
        {
            profile = default;
            Debug.LogError($"{GetType().Name} could not load Card Quality Presentation Catalog.", this);
            return false;
        }

        if (!catalog.TryGetProfile(quality, out profile))
        {
            Debug.LogError($"{GetType().Name} could not find card quality presentation profile '{quality}'.", this);
            return false;
        }

        return true;
    }

    private CardQualityPresentationCatalogSO ResolveQualityPresentationCatalog()
    {
        if (qualityPresentationCatalogOverride != null)
        {
            return qualityPresentationCatalogOverride;
        }

        return GameContentRuntime.TryGetProvider(out IGameContentProvider provider)
            ? provider.CardQualityPresentationCatalog
            : null;
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

    private static bool IsLeftButton(PointerEventData eventData)
    {
        return eventData == null || eventData.button == PointerEventData.InputButton.Left;
    }

    private static bool CanReceivePointerInteraction(CardMotionController motionController)
    {
        return motionController == null || motionController.CanReceiveInteraction;
    }
}
