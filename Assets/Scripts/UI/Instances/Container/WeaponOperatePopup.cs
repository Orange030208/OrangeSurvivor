using Orange.UIFramework;
using UnityEngine;
using UnityEngine.UI;

public class WeaponOperatePopup : InventoryOperatePopupBase
{
    [SerializeField] private Button sellButton;
    [SerializeField] private Button mergeButton;
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
        iconImage.sprite = resource.itemData.ItemIcon;
        sellPriceText.text = resource.sellPrice.ToString();

        RenderItemQuality(resource.itemData, resource.colorDependencyNumber);
        DisplayDocument(resource.infoSource);

        currentEntryId = resource.entryId;

        sellButton.onClick.RemoveListener(OnSellClicked);
        mergeButton.onClick.RemoveListener(OnMergeClicked);

        bool canMerge = WeaponLevelHelper.CanMerge(resource.colorDependencyNumber);

        sellButton.onClick.AddListener(OnSellClicked);
        if (canMerge)
        {
            mergeButton.onClick.AddListener(OnMergeClicked);
        }

        CleanClickEvent();
    }

    public override void Dispose()
    {
        base.Dispose();
        sellButton.onClick.RemoveListener(OnSellClicked);
        mergeButton.onClick.RemoveListener(OnMergeClicked);
        SellRequested = null;
        MergeRequested = null;
        currentEntryId = null;
    }

    private void OnSellClicked()
    {
        SellRequested?.Invoke(currentEntryId);
    }

    private void OnMergeClicked()
    {
        MergeRequested?.Invoke(currentEntryId);
    }
}
