using System;
using AXR.Framework.UI;
using UnityEngine;

public sealed class ShopSidebarRegionHost
{
    private readonly ShopPropertiesRegionView propertiesRegion;
    private readonly ShopInventoryRegionView inventoryRegion;

    public ShopSidebarRegionHost(
        string ownerName,
        MonoBehaviour propertiesSidebar,
        UIClickTarget propertiesToggleButton,
        Describer propertiesDescriber,
        MonoBehaviour inventorySidebar,
        UIClickTarget inventoryToggleButton)
    {
        string resolvedOwnerName = string.IsNullOrWhiteSpace(ownerName) ? nameof(ShopSidebarRegionHost) : ownerName;
        propertiesRegion = new ShopPropertiesRegionView(resolvedOwnerName, propertiesSidebar, propertiesToggleButton, propertiesDescriber);
        inventoryRegion = new ShopInventoryRegionView(resolvedOwnerName, inventorySidebar, inventoryToggleButton);
        propertiesRegion.ToggleRequested += OnPropertiesToggleRequested;
        inventoryRegion.ToggleRequested += OnInventoryToggleRequested;
    }

    public event Action PropertiesToggleRequested;
    public event Action InventoryToggleRequested;

    public void Bind(PropertiesManager propertiesManager)
    {
        propertiesRegion.Bind(propertiesManager);
        inventoryRegion.Bind();
    }

    public void Unbind()
    {
        propertiesRegion.Unbind();
        inventoryRegion.Unbind();
    }

    public void SetPropertiesVisible(bool visible)
    {
        propertiesRegion.SetVisible(visible);
    }

    public void SetInventoryVisible(bool visible)
    {
        inventoryRegion.SetVisible(visible);
    }

    public void RefreshDefaults()
    {
        propertiesRegion.RefreshDefaults();
        inventoryRegion.RefreshDefaults();
    }

    public void Kill()
    {
        propertiesRegion.Kill();
        inventoryRegion.Kill();
    }

    private void OnPropertiesToggleRequested()
    {
        PropertiesToggleRequested?.Invoke();
    }

    private void OnInventoryToggleRequested()
    {
        InventoryToggleRequested?.Invoke();
    }
}
