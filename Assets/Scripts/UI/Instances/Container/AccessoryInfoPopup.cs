public class AccessoryInfoPopup : InventoryOperatePopupBase
{
    public override void Configure(InventoryItemOperateResource resource)
    {
        if (resource.itemData == null)
        {
            throw new System.ArgumentException($"{nameof(AccessoryInfoPopup)} '{name}' received an empty item resource.");
        }

        nameText.text = resource.itemData.ItemName;
        iconImage.sprite = resource.itemData.Icon;
        RenderItemQuality(resource.itemData, resource.colorDependencyNumber);
        bottom.Display(resource.describable);
        CleanClickEvent();
    }
}
