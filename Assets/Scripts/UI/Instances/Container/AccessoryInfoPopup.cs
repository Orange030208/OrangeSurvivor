public class AccessoryInfoPopup : InventoryOperatePopupBase
{
    public override void Configure(InventoryItemOperateResource resource)
    {
        if (resource.itemData == null)
        {
            throw new System.ArgumentException($"{nameof(AccessoryInfoPopup)} '{name}' received an empty item resource.");
        }

        nameText.text = resource.itemData.ItemName;
        iconImage.sprite = resource.itemData.ItemIcon;
        RenderItemQuality(resource.itemData, resource.colorDependencyNumber);
        DisplayDocument(resource.infoSource);
        CleanClickEvent();
    }
}
