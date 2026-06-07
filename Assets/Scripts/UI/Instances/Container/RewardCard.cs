using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RewardCard :
    ViewPartBase,
    IDisposable,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerMoveHandler
{
    [FormerlySerializedAs("nameText")]
    [SerializeField] protected TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI qualityText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private GameObject iconRoot;
    [SerializeField] private Image iconImage;

    [Header("卡片音效")]
    [SerializeField] private AudioSfxKey revealSfxKey = AudioSfxKey.UpgradeCardReveal;
    [SerializeField] private AudioSfxKey selectSfxKey = AudioSfxKey.None;

    [Header("卡片动效")]
    [SerializeField] private CardMotionController motion;
    [SerializeField] private bool playRevealSfx = true;

    private CanvasGroup cardCanvasGroup;
    private Func<int, bool> submitGate;
    private int containerIndex = -1;
    private string optionId = string.Empty;
    private Action<int, string> submitRequested;
    private bool isSubmitting;
    private bool interactionLocked;
    private bool isPointerPressed;
    private bool currentOptionInteractable;
    private bool wasRaycastBlockingBeforeSubmit = true;

    public int OptionIndex => containerIndex;
    public string OptionId => optionId;

    private event Action<PointerEventData> OnClicked;

    public void Configure(RewardSelectionCardBinding resource)
    {
        containerIndex = resource.Index;
        RewardCardViewConfig option = resource.Card;
        if (option == null)
        {
            Debug.LogError($"{GetType().Name} '{name}' received a null reward card view config.", this);
            return;
        }

        optionId = option.OptionId;
        submitRequested = resource.SubmitRequested;
        currentOptionInteractable = option.Interactable;
        RenderConfig(option);

        ConfigureCardMotionForReuse();
        PlayRevealSfx();

        CleanClickEvent();
        OnClicked += _ =>
        {
            PlayCurrentSelectionFeedback();
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
        if (!currentOptionInteractable ||
            isSubmitting ||
            interactionLocked ||
            !CanReceivePointerInteraction(motionController) ||
            !IsLeftButton(eventData))
        {
            return;
        }

        if (submitGate != null && !submitGate.Invoke(containerIndex))
        {
            return;
        }

        if (submitRequested != null)
        {
            PlayCurrentSelectionFeedback();
            submitRequested.Invoke(containerIndex, optionId);
            return;
        }

        RaiseClicked(eventData);
    }

    public void Dispose()
    {
        StopSubmitRoutine();
        CleanClickEvent();
        submitRequested = null;
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

    public virtual async UniTask PlaySelectedSubmitAsync(CancellationToken cancellationToken)
    {
        isSubmitting = true;
        isPointerPressed = false;
        SetRaycastBlocking(false);
        try
        {
            CardMotionController motionController = GetMotion();
            if (motionController != null)
            {
                await motionController.PlaySelectedSubmitAsync(cancellationToken);
            }
        }
        finally
        {
            SetRaycastBlocking(wasRaycastBlockingBeforeSubmit);
            isSubmitting = false;
        }
    }

    public virtual async UniTask PlayRejectedSubmitAsync(float startDelay, CancellationToken cancellationToken)
    {
        isSubmitting = true;
        isPointerPressed = false;
        SetRaycastBlocking(false);
        try
        {
            if (startDelay > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(startDelay),
                    DelayType.UnscaledDeltaTime,
                    PlayerLoopTiming.Update,
                    cancellationToken);
            }

            CardMotionController motionController = GetMotion();
            if (motionController != null)
            {
                await motionController.PlayRejectedSubmitAsync(cancellationToken);
            }
            else
            {
                await PlayFallbackRejectedSubmitAsync(cancellationToken);
            }
        }
        finally
        {
            SetRaycastBlocking(wasRaycastBlockingBeforeSubmit);
            isSubmitting = false;
        }
    }

    protected virtual void RenderConfig(RewardCardViewConfig option)
    {
        if (titleText != null)
        {
            titleText.text = option.Title;
        }

        if (qualityText != null)
        {
            qualityText.text = ResolveQualityText(option.Tier);
        }

        if (descriptionText != null)
        {
            descriptionText.text = option.Description;
            descriptionText.gameObject.SetActive(!string.IsNullOrWhiteSpace(option.Description));
        }

        bool hasIcon = option.Icon != null;
        if (iconRoot != null)
        {
            iconRoot.SetActive(hasIcon);
        }

        if (iconImage != null)
        {
            iconImage.sprite = option.Icon;
            iconImage.enabled = hasIcon;
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

    private void PlayCurrentSelectionFeedback()
    {
        PlaySelectSfx();
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

    private void StopSubmitRoutine()
    {
        SetRaycastBlocking(wasRaycastBlockingBeforeSubmit);
        isSubmitting = false;
        interactionLocked = false;
        isPointerPressed = false;
        GetMotion()?.CancelAndReset();
    }

    private async UniTask PlayFallbackRejectedSubmitAsync(CancellationToken cancellationToken)
    {
        CanvasGroup canvasGroup = ResolveCanvasGroup();
        RectTransform rectTransform = transform as RectTransform;
        if (canvasGroup == null && rectTransform == null)
        {
            return;
        }

        Sequence sequence = DOTween.Sequence();
        if (canvasGroup != null)
        {
            sequence.Join(canvasGroup.DOFade(0f, 0.24f));
        }

        if (rectTransform != null)
        {
            sequence.Join(rectTransform.DOAnchorPosY(rectTransform.anchoredPosition.y - 96f, 0.24f));
            sequence.Join(rectTransform.DOScale(0.92f, 0.24f));
        }

        sequence.SetEase(Ease.InCubic).SetUpdate(true);
        await sequence.WaitForCompletionAsync(cancellationToken);
    }

    private CanvasGroup ResolveCanvasGroup()
    {
        if (cardCanvasGroup == null)
        {
            cardCanvasGroup = GetComponent<CanvasGroup>();
        }

        return cardCanvasGroup;
    }

    private CardMotionController GetMotion()
    {
        if (motion == null)
        {
            motion = GetComponent<CardMotionController>();
        }

        return motion;
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

    private void PlayRevealSfx()
    {
        if (!playRevealSfx || revealSfxKey == AudioSfxKey.None)
        {
            return;
        }

        AudioSfxBridge.RequestPlay(revealSfxKey);
    }

    private void PlaySelectSfx()
    {
        if (selectSfxKey == AudioSfxKey.None)
        {
            return;
        }

        AudioSfxBridge.RequestPlay(selectSfxKey);
    }

    private static bool IsLeftButton(PointerEventData eventData)
    {
        return eventData == null || eventData.button == PointerEventData.InputButton.Left;
    }

    private static bool CanReceivePointerInteraction(CardMotionController motionController)
    {
        return motionController == null || motionController.CanReceiveInteraction;
    }

    private static string ResolveQualityText(ContentTier tier)
    {
        return tier switch
        {
            ContentTier.Rare => "稀有",
            ContentTier.Epic => "史诗",
            ContentTier.Legendary => "传说",
            _ => "普通"
        };
    }
}
