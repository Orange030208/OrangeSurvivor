using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIUpgradeContainer :
    UIContainerBase<InfoAddIndex<UpgradeCardOptionSnapshot>,Describer>,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("稀有度表现")]
    [SerializeField] private UIMotionPlayer cardMotionPlayer;
    [SerializeField] private bool playRevealSfx = true;

    private CanvasGroup cardCanvasGroup;
    private Coroutine submitRoutine;
    private bool isSubmitting;
    private bool isPointerPressed;
    private bool wasRaycastBlockingBeforeSubmit = true;

    public override void Configure(InfoAddIndex<UpgradeCardOptionSnapshot> resource)
    {
        UpgradeCardOptionSnapshot option = resource.info;
        iconImage.sprite = option.Icon;
        nameText.text = option.Title;
        DefaultDescribe describable = new DefaultDescribe
        {
            Description = BuildDescription(option)
        };
        bottom.Display(describable);
        UpgradeCardRarityPresentationProfile presentationProfile = ResolveRarityPresentationProfile(option.Rarity);
        RefreshCardMotionDefaults();
        PlayRevealSfx(presentationProfile);
        CleanClickEvent();
        OnClicked += _ =>
        {
            PlaySelectSfx(presentationProfile);
            GameEventBus.Publish<UpgradeContainerClickedEvent>(new UpgradeContainerClickedEvent(resource.index));
        };
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isSubmitting)
        {
            return;
        }

        PlayMotion(UIMotionClipIds.HOVER_IN);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isSubmitting)
        {
            return;
        }

        if (isPointerPressed)
        {
            isPointerPressed = false;
            PlayMotion(UIMotionClipIds.RELEASE);
        }

        PlayMotion(UIMotionClipIds.HOVER_OUT);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isSubmitting || !IsLeftButton(eventData))
        {
            return;
        }

        isPointerPressed = true;
        PlayMotion(UIMotionClipIds.PRESS);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isSubmitting || !IsLeftButton(eventData))
        {
            return;
        }

        isPointerPressed = false;
        PlayMotion(UIMotionClipIds.RELEASE);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (isSubmitting || !IsLeftButton(eventData))
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

    private void OnDisable()
    {
        StopSubmitRoutine();
    }

    private void RefreshCardMotionDefaults()
    {
        if (cardMotionPlayer == null)
        {
            cardMotionPlayer = GetComponent<UIMotionPlayer>();
        }

        // 升级卡片可能被对象池复用，刷新默认快照可避免上一次 hover/press 的缩放或位移残留。
        cardMotionPlayer?.RefreshDefaults();
    }

    private IEnumerator SubmitAfterClickMotion(PointerEventData eventData)
    {
        isSubmitting = true;
        isPointerPressed = false;
        SetRaycastBlocking(false);

        if (cardMotionPlayer != null)
        {
            yield return cardMotionPlayer.PlayAndWait(UIMotionClipIds.CLICK_PULSE);
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
        isPointerPressed = false;
    }

    private void PlayMotion(string clipId)
    {
        if (cardMotionPlayer == null)
        {
            cardMotionPlayer = GetComponent<UIMotionPlayer>();
        }

        cardMotionPlayer?.Play(clipId);
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

    private static UpgradeCardRarityPresentationProfile ResolveRarityPresentationProfile(UpgradeCardRarity rarity)
    {
        UpgradeCardRarityPresentationCatalogSO catalog = ResourcesManager.GetUpgradeCardRarityPresentationCatalog();
        return catalog != null && catalog.TryGetProfile(rarity, out UpgradeCardRarityPresentationProfile configuredProfile)
            ? configuredProfile
            : UpgradeCardRarityPresentationCatalogSO.GetDefaultProfile(rarity);
    }

    private void PlayRevealSfx(UpgradeCardRarityPresentationProfile profile)
    {
        if (!playRevealSfx)
        {
            return;
        }

        AudioSfxBridge.RequestPlay(profile.RevealSfxKey);
    }

    private static void PlaySelectSfx(UpgradeCardRarityPresentationProfile profile)
    {
        AudioSfxKey selectSfxKey = profile.SelectSfxKey != AudioSfxKey.None
            ? profile.SelectSfxKey
            : AudioSfxKey.WoodenButtonClicked;
        AudioSfxBridge.RequestPlay(selectSfxKey);
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

    private static string GetRarityText(UpgradeCardRarity rarity)
    {
        return rarity switch
        {
            UpgradeCardRarity.Common => "普通",
            UpgradeCardRarity.Rare => "稀有",
            UpgradeCardRarity.Epic => "史诗",
            UpgradeCardRarity.Legendary => "传说",
            _ => rarity.ToString()
        };
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

            result += GetTagText(tags[i]);
        }

        return result;
    }

    private static string GetTagText(UpgradeCardTag tag)
    {
        return tag switch
        {
            UpgradeCardTag.Attack => "攻击",
            UpgradeCardTag.Defense => "防御",
            UpgradeCardTag.Critical => "暴击",
            UpgradeCardTag.AttackSpeed => "攻速",
            UpgradeCardTag.MoveSpeed => "移动",
            UpgradeCardTag.Pickup => "拾取",
            UpgradeCardTag.Economy => "经济",
            UpgradeCardTag.Weapon => "武器",
            UpgradeCardTag.Melee => "近战",
            UpgradeCardTag.Ranged => "远程",
            UpgradeCardTag.Projectile => "投射物",
            UpgradeCardTag.Recovery => "回复",
            UpgradeCardTag.LowHealth => "低血",
            UpgradeCardTag.AreaDamage => "范围",
            _ => tag.ToString()
        };
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
