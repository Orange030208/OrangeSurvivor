using TMPro;
using UnityEngine;

public class InventoryItemOperateContainer : UIContainerBase<InventoryItemOperateResource, ExtraInfoDescriber>
{
    [SerializeField] private UIClickTarget sellButton;
    [SerializeField] private UIClickTarget mergeButton;
    [SerializeField] private TextMeshProUGUI sellPriceText;
    [SerializeField] private GameObject sellSection;
    [SerializeField] private GameObject mergeSection;

    private string currentEntryId;

    public event System.Action<string> CloseRequested;
    public event System.Action<string> SellRequested;
    public event System.Action<string> MergeRequested;

    public override void Configure(InventoryItemOperateResource resource)
    {
        if (resource.itemData == null)
        {
            return;
        }

        if (resource.itemData.ItemType == ItemType.Weapon)
        {
            nameText.text = ItemDisplayHelper.GetWeaponDisplayName(resource.itemData.ItemName, resource.colorDependencyNumber);
        }
        else
        {
            nameText.text = resource.itemData.ItemName;
        }

        if (sellPriceText != null)
        {
            sellPriceText.text = resource.sellPrice.ToString();
        }

        RenderColor(resource.itemData, resource.colorDependencyNumber);
        bottom.Display(resource.itemData);

        currentEntryId = resource.entryId;

        sellButton.OnClicked -= OnSellClicked;
        mergeButton.OnClicked -= OnMergeClicked;

        bool isWeapon = resource.itemData.ItemType == ItemType.Weapon;
        bool canMerge = isWeapon && WeaponLevelHelper.CanMerge(resource.colorDependencyNumber);

        if (sellSection != null)
        {
            sellSection.SetActive(isWeapon);
        }
        else
        {
            sellButton.gameObject.SetActive(isWeapon);
        }

        if (mergeSection != null)
        {
            mergeSection.SetActive(canMerge);
        }
        else
        {
            mergeButton.gameObject.SetActive(canMerge);
        }

        if (isWeapon)
        {
            sellButton.OnClicked += OnSellClicked;
        }

        if (canMerge)
        {
            mergeButton.OnClicked += OnMergeClicked;
        }

        CleanClickEvent();
        OnClicked += _ =>
        {
            CloseRequested?.Invoke(resource.entryId);
        };
    }

    public override void Dispose()
    {
        base.Dispose();
        sellButton.OnClicked -= OnSellClicked;
        mergeButton.OnClicked -= OnMergeClicked;
        CloseRequested = null;
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
