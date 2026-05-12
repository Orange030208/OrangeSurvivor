using Orange.UIFramework;

public sealed class ShopInventoryPopupContext
{
    public ShopInventoryPopupContext(InventoryOperateManager inventoryOperateManager, UIManager uiManager)
    {
        InventoryOperateManager = inventoryOperateManager
            ?? throw new System.ArgumentNullException(nameof(inventoryOperateManager));
        UIManager = uiManager
            ?? throw new System.ArgumentNullException(nameof(uiManager));
    }

    public InventoryOperateManager InventoryOperateManager { get; }
    public UIManager UIManager { get; }
}
