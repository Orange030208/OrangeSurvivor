using System;
using UnityEngine;

public sealed class ShopInventoryRegionView : ISidebarRegion
{
    private readonly SidebarToggleRegionView toggleRegion;

    public ShopInventoryRegionView(
        string ownerName,
        MonoBehaviour sidebar,
        UIClickTarget toggleButton)
    {
        string resolvedOwnerName = string.IsNullOrWhiteSpace(ownerName) ? nameof(ShopInventoryRegionView) : ownerName;
        toggleRegion = new SidebarToggleRegionView(
            nameof(ShopUIPage),
            resolvedOwnerName,
            "inventory sidebar",
            "inventory toggle button",
            sidebar,
            toggleButton);
        toggleRegion.ToggleRequested += OnToggleRequested;
    }

    public event Action ToggleRequested;

    public void Bind()
    {
        toggleRegion.Bind();
    }

    public void Unbind()
    {
        toggleRegion.Unbind();
    }

    public void SetVisible(bool visible)
    {
        toggleRegion.SetVisible(visible);
    }

    public void RefreshDefaults()
    {
        toggleRegion.RefreshDefaults();
    }

    public void Kill()
    {
        toggleRegion.Kill();
    }

    private void OnToggleRequested()
    {
        ToggleRequested?.Invoke();
    }
}
