using AXR.Framework.UI;
using UnityEngine;

public class WeaponOperatePopup : InventoryOperatePopupBase
{
    [SerializeField] private UIClickTarget sellButton;
    [SerializeField] private UIClickTarget mergeButton;
    [SerializeField] private TMPro.TextMeshProUGUI sellPriceText;

    private string currentEntryId;

    public event System.Action<string> SellRequested;
    public event System.Action<string> MergeRequested;

    public override void Configure(InventoryItemOperateResource resource)
    {
        if (resource.itemData == null)
        {
            throw new System.ArgumentException($"{nameof(WeaponOperatePopup)} '{name}' received an empty item resource.");
        }

        nameText.text = ItemDisplayHelper.GetWeaponDisplayName(resource.itemData.ItemName, resource.colorDependencyNumber);
        iconImage.sprite = resource.itemData.Icon;
        sellPriceText.text = resource.sellPrice.ToString();

        RenderItemQuality(resource.itemData, resource.colorDependencyNumber);
        bottom.Display(resource.describable);

        currentEntryId = resource.entryId;

        sellButton.OnClicked -= OnSellClicked;
        mergeButton.OnClicked -= OnMergeClicked;

        bool canMerge = WeaponLevelHelper.CanMerge(resource.colorDependencyNumber);

        sellButton.OnClicked += OnSellClicked;
        if (canMerge)
        {
            mergeButton.OnClicked += OnMergeClicked;
        }

        CleanClickEvent();
    }

    public override void Dispose()
    {
        base.Dispose();
        sellButton.OnClicked -= OnSellClicked;
        mergeButton.OnClicked -= OnMergeClicked;
        SellRequested = null;
        MergeRequested = null;
        currentEntryId = null;
    }

    private void OnSellClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        SellRequested?.Invoke(currentEntryId);
    }

    private void OnMergeClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        MergeRequested?.Invoke(currentEntryId);
    }
}
