using System;
using Orange.UIFramework;
using UnityEngine;

public sealed class ShopInventorySidebarView
{
    private readonly SidebarToggleView toggleView;

    public ShopInventorySidebarView(
        string ownerName,
        MonoBehaviour sidebar,
        UIClickTarget toggleButton)
    {
        string resolvedOwnerName = string.IsNullOrWhiteSpace(ownerName) ? nameof(ShopInventorySidebarView) : ownerName;
        toggleView = new SidebarToggleView(
            nameof(ShopUIPage),
            resolvedOwnerName,
            "inventory sidebar",
            "inventory toggle button",
            sidebar,
            toggleButton);
        toggleView.ToggleRequested += OnToggleRequested;
    }

    public event Action ToggleRequested;

    public void Bind()
    {
        toggleView.Bind();
    }

    public void Unbind()
    {
        toggleView.Unbind();
    }

    public void SetVisible(bool visible)
    {
        toggleView.SetVisible(visible);
    }

    public void RefreshDefaults()
    {
        toggleView.RefreshDefaults();
    }

    public void Kill()
    {
        toggleView.Kill();
    }

    private void OnToggleRequested()
    {
        ToggleRequested?.Invoke();
    }
}
