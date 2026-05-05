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

    public static void Bind(Component host, ref InventoryUI inventoryUI, IInventoryFacadeContext context)
    {
        Bind(host, ref inventoryUI, context, null);
    }

    public static void Bind(Component host, ref InventoryUI inventoryUI, IInventoryFacadeContext context, UIManager uiManager)
    {
        WarmUp(host, ref inventoryUI);
        if (inventoryUI == null || context == null)
        {
            return;
        }

        inventoryUI.ConfigureUIManager(uiManager);
        inventoryUI.ConfigureFacade(context.InventoryFacade);
    }

    public static void Release(InventoryUI inventoryUI)
    {
        inventoryUI?.ReleaseConfiguredFacade();
    }
}
