using System;
using Orange.UIFramework;
using UnityEngine;

public sealed class ShopPropertiesRegionView
{
    private readonly SidebarToggleRegionView toggleRegion;
    private readonly PropertiesDescriberBinding describerBinding;

    public ShopPropertiesRegionView(
        string ownerName,
        MonoBehaviour sidebar,
        UIClickTarget toggleButton,
        Describer propertiesDescriber)
    {
        string resolvedOwnerName = string.IsNullOrWhiteSpace(ownerName) ? nameof(ShopPropertiesRegionView) : ownerName;
        toggleRegion = new SidebarToggleRegionView(
            nameof(ShopUIPage),
            resolvedOwnerName,
            "properties sidebar",
            "properties toggle button",
            sidebar,
            toggleButton);
        toggleRegion.ToggleRequested += OnToggleRequested;
        describerBinding = new PropertiesDescriberBinding(nameof(ShopUIPage), resolvedOwnerName, "properties describer", propertiesDescriber);
    }

    public event Action ToggleRequested;

    public void Bind(PropertiesManager newPropertiesManager)
    {
        toggleRegion.Bind();
        describerBinding.Bind(newPropertiesManager);
    }

    public void Unbind()
    {
        toggleRegion.Unbind();
        describerBinding.Unbind();
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
