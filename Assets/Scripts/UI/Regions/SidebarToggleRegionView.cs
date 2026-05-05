using Orange.UIFramework;
using System;
using UnityEngine;

public sealed class SidebarToggleRegionView
{
    private readonly SidebarRegionMotion motion;
    private readonly UIClickTarget toggleButton;

    private bool bound;

    public SidebarToggleRegionView(
        string ownerTypeName,
        string ownerName,
        string missingSidebarFieldName,
        string missingToggleButtonFieldName,
        MonoBehaviour sidebar,
        UIClickTarget toggleButton)
    {
        motion = new SidebarRegionMotion(ownerTypeName, ownerName, missingSidebarFieldName, sidebar);
        this.toggleButton = toggleButton ?? throw new MissingReferenceException($"{ownerTypeName} '{ownerName}' is missing {missingToggleButtonFieldName}.");
    }

    public event Action ToggleRequested;

    public void Bind()
    {
        if (bound)
        {
            return;
        }

        toggleButton.OnClicked += OnToggleButtonClicked;
        bound = true;
    }

    public void Unbind()
    {
        if (!bound)
        {
            return;
        }

        toggleButton.OnClicked -= OnToggleButtonClicked;
        bound = false;
    }

    public void SetVisible(bool visible)
    {
        motion.SetVisible(visible);
    }

    public void RefreshDefaults()
    {
        motion.RefreshDefaults();
    }

    public void Kill()
    {
        motion.Kill();
    }

    private void OnToggleButtonClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        ToggleRequested?.Invoke();
    }
}
