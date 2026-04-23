using DG.Tweening;
using UnityEngine;

public sealed class SidebarRegionMotion
{
    private readonly UISidebarRevealMotion sidebar;

    public SidebarRegionMotion(string ownerTypeName, string ownerName, string missingFieldName, UISidebarRevealMotion sidebar)
    {
        string resolvedOwnerTypeName = string.IsNullOrWhiteSpace(ownerTypeName) ? nameof(SidebarRegionMotion) : ownerTypeName;
        string resolvedOwnerName = string.IsNullOrWhiteSpace(ownerName) ? resolvedOwnerTypeName : ownerName;
        string resolvedMissingFieldName = string.IsNullOrWhiteSpace(missingFieldName) ? nameof(UISidebarRevealMotion) : missingFieldName;

        this.sidebar = sidebar ?? throw new MissingReferenceException($"{resolvedOwnerTypeName} '{resolvedOwnerName}' is missing {resolvedMissingFieldName}.");
    }

    public void SetVisible(bool visible)
    {
        sidebar.Play(visible ? UIMotionAction.Show : UIMotionAction.Hide);
    }

    public Tween PlayHide()
    {
        return sidebar.Play(UIMotionAction.Hide);
    }

    public void RefreshDefaults()
    {
        sidebar.RefreshDefaults();
    }

    public void SetHiddenImmediate()
    {
        sidebar.SetImmediate(UIMotionAction.Hide);
    }

    public void Kill()
    {
        sidebar.Kill();
    }

    public void ConfigureTimings(float showDuration, Ease showEase, float hideDuration, Ease hideEase)
    {
        sidebar.ConfigureTimings(showDuration, showEase, hideDuration, hideEase);
    }
}
