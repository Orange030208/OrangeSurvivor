using System;
using Orange.UIFramework;
using UnityEngine;

public sealed class ShopPropertiesSidebarView
{
    private readonly SidebarToggleView toggleView;
    private readonly PropertiesDescriberBinding describerBinding;

    public ShopPropertiesSidebarView(
        string ownerName,
        MonoBehaviour sidebar,
        UIClickTarget toggleButton,
        Describer propertiesDescriber)
    {
        string resolvedOwnerName = string.IsNullOrWhiteSpace(ownerName) ? nameof(ShopPropertiesSidebarView) : ownerName;
        toggleView = new SidebarToggleView(
            nameof(ShopUIPage),
            resolvedOwnerName,
            "properties sidebar",
            "properties toggle button",
            sidebar,
            toggleButton);
        toggleView.ToggleRequested += OnToggleRequested;
        describerBinding = new PropertiesDescriberBinding(nameof(ShopUIPage), resolvedOwnerName, "properties describer", propertiesDescriber);
    }

    public event Action ToggleRequested;

    public void Bind(PropertiesManager newPropertiesManager)
    {
        toggleView.Bind();
        describerBinding.Bind(newPropertiesManager);
    }

    public void Unbind()
    {
        toggleView.Unbind();
        describerBinding.Unbind();
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
