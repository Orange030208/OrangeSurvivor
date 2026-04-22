using UnityEngine;

public class AccessoryInfoPopup : UIContainerBase<InventoryItemOperateResource, ExtraInfoDescriber>
{
    public override void Configure(InventoryItemOperateResource resource)
    {
        nameText.text = resource.itemData.ItemName;
        iconImage.sprite = resource.itemData.Icon;
        RenderColor(resource.itemData, resource.colorDependencyNumber);
        bottom.Display(resource.itemData);
        CleanClickEvent();
    }
}
