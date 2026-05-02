using AXR.Framework.UI;
using DG.Tweening;
using UnityEngine;

public sealed class SidebarRegionMotion
{
    private readonly MonoBehaviour sidebarBehaviour;
    private readonly IUIRuntimeMotion runtimeMotion;

    public SidebarRegionMotion(string ownerTypeName, string ownerName, string missingFieldName, MonoBehaviour sidebar)
    {
        string resolvedOwnerTypeName = string.IsNullOrWhiteSpace(ownerTypeName) ? nameof(SidebarRegionMotion) : ownerTypeName;
        string resolvedOwnerName = string.IsNullOrWhiteSpace(ownerName) ? resolvedOwnerTypeName : ownerName;
        string resolvedMissingFieldName = string.IsNullOrWhiteSpace(missingFieldName) ? nameof(UIMotionPlayer) : missingFieldName;

        sidebarBehaviour = sidebar ?? throw new MissingReferenceException($"{resolvedOwnerTypeName} '{resolvedOwnerName}' is missing {resolvedMissingFieldName}.");
        runtimeMotion = ResolveRuntimeMotion(sidebarBehaviour)
                        ?? throw new MissingComponentException($"{resolvedOwnerTypeName} '{resolvedOwnerName}' expects {resolvedMissingFieldName} to implement {nameof(IUIRuntimeMotion)}.");
    }

    public void SetVisible(bool visible)
    {
        runtimeMotion.Play(visible ? UIMotionClipIds.SHOW : UIMotionClipIds.HIDE);
    }

    public Tween PlayHide()
    {
        return runtimeMotion.Play(UIMotionClipIds.HIDE);
    }

    public void RefreshDefaults()
    {
        runtimeMotion.RefreshDefaults();
    }

    public void SetHiddenImmediate()
    {
        runtimeMotion.SetImmediate(UIMotionClipIds.HIDE);
    }

    public void Kill()
    {
        runtimeMotion.Kill();
    }

    public void ConfigureTimings(float showDuration, Ease showEase, float hideDuration, Ease hideEase)
    {
    }

    private static IUIRuntimeMotion ResolveRuntimeMotion(MonoBehaviour source)
    {
        if (source is IUIRuntimeMotion directMotion)
        {
            return directMotion;
        }

        MonoBehaviour[] behaviours = source.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IUIRuntimeMotion motion)
            {
                return motion;
            }
        }

        return null;
    }
}
