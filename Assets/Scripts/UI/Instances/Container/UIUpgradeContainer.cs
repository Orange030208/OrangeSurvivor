using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIUpgradeContainer :
    UIContainerBase<InfoAddIndex<UpgradeCardOptionSnapshot>,Describer>,
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
    private Coroutine submitRoutine;
    private Func<int, bool> submitGate;
    private int containerIndex = -1;
    private bool isSubmitting;
    private bool interactionLocked;
    private bool isPointerPressed;
    private bool wasRaycastBlockingBeforeSubmit = true;

    public override void Configure(InfoAddIndex<UpgradeCardOptionSnapshot> resource)
    {
        containerIndex = resource.index;
        UpgradeCardOptionSnapshot option = resource.info;
        iconImage.sprite = option.Icon;
        nameText.text = option.Title;
        bottom.Display(new UpgradeCardDisplayInfo(option, BuildDescription(option)));
        bool hasPresentationProfile = TryResolveQualityPresentationProfile(
            option.Rarity,
            out CardQualityPresentationProfile presentationProfile);
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
            if (hasPresentationProfile)
            {
                PlaySelectSfx(presentationProfile);
            }

            GameEventBus.Publish<UpgradeContainerClickedEvent>(new UpgradeContainerClickedEvent(resource.index));
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

        // 整组锁定时立刻收回未选中卡片的按压/悬停残留，避免视觉状态卡住。
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

        if (submitRoutine != null)
        {
            StopCoroutine(submitRoutine);
        }

        submitRoutine = StartCoroutine(SubmitAfterClickMotion(eventData));
    }

    public override void Dispose()
    {
        StopSubmitRoutine();
        base.Dispose();
    }

    public IEnumerator PlayRefreshOutAndWait()
    {
        CardMotionController motionController = GetCardMotionController();
        if (motionController == null)
        {
            yield break;
        }

        yield return motionController.PlayRefreshOutAndWait();
    }

    private void OnDisable()
    {
        StopSubmitRoutine();
    }

    private void ConfigureCardMotionForReuse()
    {
        // 升级卡片可能被对象池复用，交给专用动效控制器统一处理默认快照与残留状态。
        GetCardMotionController()?.ConfigureForReuse();
    }

    private IEnumerator SubmitAfterClickMotion(PointerEventData eventData)
    {
        isSubmitting = true;
        isPointerPressed = false;
        SetRaycastBlocking(false);

        CardMotionController motionController = GetCardMotionController();
        if (motionController != null)
        {
            yield return motionController.PlaySelectAndWait();
            motionController.ResetToRest();
        }

        SetRaycastBlocking(wasRaycastBlockingBeforeSubmit);
        isSubmitting = false;
        submitRoutine = null;

        if (isActiveAndEnabled)
        {
            RaiseClicked(eventData);
        }
    }

    private void StopSubmitRoutine()
    {
        if (submitRoutine != null)
        {
            StopCoroutine(submitRoutine);
            submitRoutine = null;
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

    private static bool TryResolveQualityPresentationProfile(
        UpgradeCardRarity rarity,
        out CardQualityPresentationProfile profile)
    {
        CardQualityPresentationCatalogSO catalog = ResourcesManager.GetCardQualityPresentationCatalog();
        if (catalog == null)
        {
            profile = default;
            Debug.LogError($"{nameof(UIUpgradeContainer)} could not load Card Quality Presentation Catalog.");
            return false;
        }

        CardQuality quality = CardQualityResolver.FromUpgradeCardRarity(rarity);
        if (!catalog.TryGetProfile(quality, out profile))
        {
            Debug.LogError($"{nameof(UIUpgradeContainer)} could not find card quality presentation profile '{quality}'.");
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

    private static string BuildDescription(UpgradeCardOptionSnapshot option)
    {
        string description = option.Description;
        string rarityText = GetRarityText(option.Rarity);
        string pickText = option.HasPickLimit && option.MaxPickCount > 1
            ? $"\n已选择 {option.PickCount}/{option.MaxPickCount}"
            : string.Empty;
        string tagText = BuildTagText(option.Tags);
        return $"{rarityText}{tagText}\n{description}{pickText}";
    }

    private sealed class UpgradeCardDisplayInfo : IDescribable
    {
        private readonly UpgradeCardOptionSnapshot option;

        public UpgradeCardDisplayInfo(UpgradeCardOptionSnapshot option, string description)
        {
            this.option = option;
            Description = description;
        }

        public string Title => option.Title;
        public Sprite Icon => option.Icon;
        public string Description { get; }

        public System.Collections.Generic.IEnumerable<DescriptorInfo> GetExtraInfos()
        {
            if (string.IsNullOrWhiteSpace(Description))
            {
                yield break;
            }

            yield return new DescriptorInfo(string.Empty, Description);
        }
    }

    private static string GetRarityText(UpgradeCardRarity rarity)
    {
        return ItemDescriptionUtility.FormatRarity(rarity);
    }

    private static string BuildTagText(UpgradeCardTag[] tags)
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

            result += ItemDescriptionUtility.FormatUpgradeCardTag(tags[i]);
        }

        return result;
    }
}

public struct InfoAddIndex<T>
{
    public T info;
    public int index;

    public InfoAddIndex(T info, int index)
    {
        this.info = info;
        this.index = index;
    }
}
