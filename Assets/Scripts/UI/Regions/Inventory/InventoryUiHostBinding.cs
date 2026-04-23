using UnityEngine;

public static class InventoryUiHostBinding
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
        WarmUp(host, ref inventoryUI);
        if (inventoryUI == null || context == null)
        {
            return;
        }

        inventoryUI.ConfigureFacade(context.InventoryFacade);
    }

    public static void Release(InventoryUI inventoryUI)
    {
        inventoryUI?.ReleaseConfiguredFacade();
    }
}
