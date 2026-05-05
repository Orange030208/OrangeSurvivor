using System;
using Orange.UIFramework;
using UnityEngine;

public sealed class ShopSidebarHost
{
    private readonly ShopPropertiesSidebarView propertiesView;
    private readonly ShopInventorySidebarView inventoryView;

    public ShopSidebarHost(
        string ownerName,
        MonoBehaviour propertiesSidebar,
        UIClickTarget propertiesToggleButton,
        Describer propertiesDescriber,
        MonoBehaviour inventorySidebar,
        UIClickTarget inventoryToggleButton)
    {
        string resolvedOwnerName = string.IsNullOrWhiteSpace(ownerName) ? nameof(ShopSidebarHost) : ownerName;
        propertiesView = new ShopPropertiesSidebarView(resolvedOwnerName, propertiesSidebar, propertiesToggleButton, propertiesDescriber);
        inventoryView = new ShopInventorySidebarView(resolvedOwnerName, inventorySidebar, inventoryToggleButton);
        propertiesView.ToggleRequested += OnPropertiesToggleRequested;
        inventoryView.ToggleRequested += OnInventoryToggleRequested;
    }

    public event Action PropertiesToggleRequested;
    public event Action InventoryToggleRequested;

    public void Bind(PropertiesManager propertiesManager)
    {
        propertiesView.Bind(propertiesManager);
        inventoryView.Bind();
    }

    public void Unbind()
    {
        propertiesView.Unbind();
        inventoryView.Unbind();
    }

    public void SetPropertiesVisible(bool visible)
    {
        propertiesView.SetVisible(visible);
    }

    public void SetInventoryVisible(bool visible)
    {
        inventoryView.SetVisible(visible);
    }

    public void RefreshDefaults()
    {
        propertiesView.RefreshDefaults();
        inventoryView.RefreshDefaults();
    }

    public void Kill()
    {
        propertiesView.Kill();
        inventoryView.Kill();
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
