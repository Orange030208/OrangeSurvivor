using Orange.UIFramework;
using UnityEngine;

public static class InventoryUiBinder
{
    public static void WarmUp(Component host, ref InventoryUI inventoryUI)
    {
        if (host == null || inventoryUI != null)
        {
            return;
        }

        inventoryUI = host.GetComponentInChildren<InventoryUI>(true);
    }

    public static void Bind(Component host, ref InventoryUI inventoryUI, InventoryOperateManager inventoryOperateManager)
    {
        Bind(host, ref inventoryUI, inventoryOperateManager, null);
    }

    public static void Bind(Component host, ref InventoryUI inventoryUI, InventoryOperateManager inventoryOperateManager, UIManager uiManager)
    {
        WarmUp(host, ref inventoryUI);
        if (inventoryUI == null || inventoryOperateManager == null)
        {
            return;
        }

        inventoryUI.ConfigureUIManager(uiManager);
        inventoryUI.ConfigureInventoryOperateManager(inventoryOperateManager);
    }

    public static void Release(InventoryUI inventoryUI)
    {
        inventoryUI?.ReleaseConfiguredInventoryOperateManager();
    }
}
