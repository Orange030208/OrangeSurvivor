using Orange.UIFramework;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ShopItemContainer : ViewPartBase, IDisposable, IPointerClickHandler
{
    [Header("基础信息")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private ExtraInfoDescriber bottom;

    [Header("卡片品质表现")]
    [FormerlySerializedAs("cardQualityVisualController")]
    [SerializeField] private CardQualityVisualController qualityVisual;
    [SerializeField] private CardQualityPresentationCatalogSO qualityPresentationCatalogOverride;

    [Header("商店操作")]
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button lockButton;
    [SerializeField] private Image lockImage;
    [SerializeField] private Sprite lockSprite, unlockSprite;

    [Header("卡片动效")]
    [FormerlySerializedAs("cardMotionController")]
    [SerializeField] private CardMotionController motion;

    private int currentIndex = -1;

    public event Action<int> BuyRequested;
    public event Action<int> LockToggleRequested;

    public void Configure(InfoAddIndex<ShopItemData> resource)
    {
        Configure(resource, true);
    }

    public void Configure(InfoAddIndex<ShopItemData> resource, bool playReveal)
    {
        Configure(resource, playReveal, refreshMotion: true);
    }

    public void Configure(InfoAddIndex<ShopItemData> resource, bool playReveal, bool refreshMotion)
    {
        ShopItemData shopItem = resource.info;
        ItemDataSO itemData = shopItem.ItemData;

        if (itemData == null)
        {
            throw new InvalidOperationException($"{nameof(ShopItemContainer)} '{name}' received a shop item without {nameof(ItemDataSO)}.");
        }

        int colorDependency;
        if (itemData is AccessoryDataSO accessoryData)
        {
            colorDependency = accessoryData.Rarity;
            SetNameText(itemData.ItemName);
            bottom?.Display(accessoryData);
        }
        else if (itemData is WeaponDataSO weaponData)
        {
            colorDependency = shopItem.Level;
            SetNameText(ItemDisplayHelper.GetWeaponDisplayName(itemData.ItemName, shopItem.Level));
            bottom?.Display(new WeaponLevelDescribable(weaponData, shopItem.Level));
        }
        else
        {
            throw new InvalidOperationException($"{nameof(ShopItemContainer)} '{name}' does not support item type '{itemData.GetType().Name}'.");
        }

        if (lockImage != null)
        {
            lockImage.sprite = shopItem.Lock ? lockSprite : unlockSprite;
        }

        if (priceText != null)
        {
            priceText.text = shopItem.GetPrice().ToString();
        }

        if (iconImage != null)
        {
            iconImage.sprite = itemData.ItemIcon;
            iconImage.enabled = itemData.ItemIcon != null;
        }

        RenderItemQuality(itemData, colorDependency);

        RemoveButtonListeners();

        currentIndex = resource.index;

        if (refreshMotion)
        {
            ConfigureCardMotionForReuse(playReveal);
        }

        AddButtonListeners();
    }

    public void Dispose()
    {
        CleanUp();
    }

    public void CleanUp()
    {
        RemoveButtonListeners();
        BuyRequested = null;
        LockToggleRequested = null;
        currentIndex = -1;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (buyButton != null || currentIndex < 0 || eventData == null || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        OnBuyButtonClicked();
    }

    private void OnBuyButtonClicked()
    {
        if (currentIndex < 0)
        {
            return;
        }

        BuyRequested?.Invoke(currentIndex);
    }

    private void OnLockButtonClicked()
    {
        if (currentIndex < 0)
        {
            return;
        }

        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
        LockToggleRequested?.Invoke(currentIndex);
    }

    private void SetNameText(string text)
    {
        if (nameText != null)
        {
            nameText.text = text;
        }
    }

    private void RenderItemQuality(ItemDataSO itemData, int qualityValue)
    {
        CardQuality quality = CardQualityResolver.FromItem(itemData, qualityValue);
        CardQualityPresentationCatalogSO catalog = ResolveQualityPresentationCatalog();
        if (catalog == null)
        {
            Debug.LogWarning($"{nameof(ShopItemContainer)} '{name}' could not resolve a card quality catalog for quality '{quality}'.", this);
            return;
        }

        if (!catalog.TryGetProfile(quality, out CardQualityPresentationProfile profile))
        {
            Debug.LogWarning($"{nameof(ShopItemContainer)} '{name}' could not resolve card quality '{quality}'.", this);
            return;
        }

        if (qualityVisual == null)
        {
            qualityVisual = GetComponent<CardQualityVisualController>();
        }

        if (qualityVisual == null)
        {
            Debug.LogWarning($"{nameof(ShopItemContainer)} '{name}' is missing {nameof(CardQualityVisualController)}; quality '{quality}' will not be rendered.", this);
            return;
        }

        qualityVisual.Apply(profile);
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

    private void AddButtonListeners()
    {
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnBuyButtonClicked);
        }

        if (lockButton != null)
        {
            lockButton.onClick.AddListener(OnLockButtonClicked);
        }
    }

    private void RemoveButtonListeners()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(OnBuyButtonClicked);
        }

        if (lockButton != null)
        {
            lockButton.onClick.RemoveListener(OnLockButtonClicked);
        }
    }

    private void ConfigureCardMotionForReuse(bool playReveal)
    {
        if (motion == null)
        {
            motion = GetComponent<CardMotionController>();
        }

        motion?.ConfigureForReuse(playReveal);
    }

    private void OnDestroy()
    {
        Dispose();
    }
}
