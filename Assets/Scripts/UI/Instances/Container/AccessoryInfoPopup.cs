using UnityEngine;

public class AccessoryInfoPopup : UIContainerBase<InventoryItemOperateResource, ExtraInfoDescriber>
{
    public override void Configure(InventoryItemOperateResource resource)
    {
        nameText.text = resource.itemData.ItemName;
        iconImage.sprite = resource.itemData.Icon;
        RenderItemQuality(resource.itemData, resource.colorDependencyNumber);
        bottom.Display(resource.describable);
        CleanClickEvent();
    }
}
