using Orange.UIFramework;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopItemContainer : ViewPartBase, IDisposable, IPointerClickHandler, IInfoDocumentSource
{
    [Serializable]
    private sealed class ShopItemTierConsumer : IContentTierConsumer
    {
        [SerializeField] private Image iconFrameImage;
        [SerializeField] private Image currencyFrameImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TierColorPaletteSO colorPalette;

        public bool Consume(ContentTier tier)
        {
            if (colorPalette == null)
            {
                return false;
            }

            Apply(colorPalette.GetColor(tier));
            return true;
        }

        private void Apply(Color color)
        {
            if (iconFrameImage != null)
            {
                iconFrameImage.color = color;
            }

            if (titleText != null)
            {
                titleText.color = color;
            }

            if (currencyFrameImage != null)
            {
                currencyFrameImage.color = color;
            }
        }

    }

    [Header("基础信息")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;

    [Header("售罄表现")]
    [SerializeField] private GameObject soldOutOverlayPrefab;

    [Header("商店操作")]
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button lockButton;
    [SerializeField] private Image lockImage;
    [SerializeField] private Sprite lockSprite, unlockSprite;

    [Header("商店商品档位表现")]
    [SerializeField] private ShopItemTierConsumer tierConsumer = new();

    private int currentIndex = -1;
    private ShopOfferViewData currentShopOffer;
    private bool isSoldOut;
    private GameObject soldOutOverlayInstance;
    private bool missingSoldOutOverlayLogged;

    public event Action<int> BuyRequested;
    public event Action<int> LockToggleRequested;

    private void Awake()
    {
        SetSoldOutState(false);
    }

    public void Configure(InfoAddIndex<ShopOfferViewData> resource)
    {
        ShopOfferViewData shopOffer = resource.info;

        if (string.IsNullOrWhiteSpace(shopOffer.DisplayName))
        {
            throw new InvalidOperationException($"{nameof(ShopItemContainer)} '{name}' received a shop offer without display name.");
        }

        SetNameText(shopOffer.DisplayName);

        if (lockImage != null)
        {
            lockImage.sprite = shopOffer.IsLocked ? lockSprite : unlockSprite;
        }

        if (priceText != null)
        {
            priceText.richText = true;
            priceText.text = BuildPriceText(shopOffer);
        }

        if (iconImage != null)
        {
            iconImage.sprite = shopOffer.Icon;
            iconImage.enabled = shopOffer.Icon != null;
        }

        if (!tierConsumer.Consume(shopOffer.Tier))
        {
            Debug.LogWarning($"{nameof(ShopItemContainer)} '{name}' could not resolve shop item tier '{shopOffer.Tier}'.", this);
        }

        RemoveButtonListeners();

        currentIndex = resource.index;
        currentShopOffer = shopOffer;
        SetSoldOutState(shopOffer.IsSoldOut);
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
        currentShopOffer = default;
        SetSoldOutState(false);
    }

    public InfoDocument BuildInfoDocument()
    {
        return currentShopOffer.InfoDocument;
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
        if (currentIndex < 0 || isSoldOut)
        {
            return;
        }

        BuyRequested?.Invoke(currentIndex);
    }

    private void OnLockButtonClicked()
    {
        if (currentIndex < 0 || isSoldOut)
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

    private static string BuildPriceText(ShopOfferViewData shopOffer)
    {
        if (shopOffer.OriginalPrice > shopOffer.Price)
        {
            return $"<s><color=#8F8F8F>{shopOffer.OriginalPrice}</color></s> <color=#FFD84A>{shopOffer.Price}</color>";
        }

        return shopOffer.Price.ToString();
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

    private void SetSoldOutState(bool soldOut)
    {
        // 售罄只管理遮罩与交互，不回写 tier 颜色，避免两层表现相互覆盖。
        isSoldOut = soldOut;

        if (soldOut && soldOutOverlayInstance == null)
        {
            EnsureSoldOutOverlayInstance();
        }

        if (soldOutOverlayInstance != null)
        {
            soldOutOverlayInstance.SetActive(soldOut);
            if (soldOut)
            {
                soldOutOverlayInstance.transform.SetAsLastSibling();
            }
        }

        SetButtonInteractable(buyButton, !soldOut);
        SetButtonInteractable(lockButton, !soldOut);
    }

    private void EnsureSoldOutOverlayInstance()
    {
        if (soldOutOverlayInstance != null)
        {
            return;
        }

        if (soldOutOverlayPrefab == null)
        {
            if (!missingSoldOutOverlayLogged)
            {
                Debug.LogWarning($"{nameof(ShopItemContainer)} '{name}' is missing {nameof(soldOutOverlayPrefab)}; sold out state will not show the overlay.", this);
                missingSoldOutOverlayLogged = true;
            }

            return;
        }

        soldOutOverlayInstance = Instantiate(soldOutOverlayPrefab, transform, false);
        soldOutOverlayInstance.transform.localScale = Vector3.one;
        soldOutOverlayInstance.SetActive(false);
        soldOutOverlayInstance.transform.SetAsLastSibling();
    }

    private static void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    private void OnDestroy()
    {
        Dispose();
    }
}
