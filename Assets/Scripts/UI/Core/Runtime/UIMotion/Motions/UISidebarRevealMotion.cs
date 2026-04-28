using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 专用于侧边栏/抽屉类 UI：负责从边缘滑入滑出，并可选附带淡出。
/// </summary>
public class UISidebarRevealMotion : UIRevealMotion
{
    private const string PANEL_OPTION = "Sidebar/Panel";
    private const string PANEL_NO_OVERSHOOT_OPTION = "Sidebar/Panel No Overshoot";
    private const string PANEL_SOFT_OPTION = "Sidebar/Panel Soft";
    private const string PANEL_SOFT_NO_OVERSHOOT_OPTION = "Sidebar/Panel Soft No Overshoot";
    private const string PANEL_FADE_OPTION = "Sidebar/Panel Fade";
    private const string PANEL_FADE_NO_OVERSHOOT_OPTION = "Sidebar/Panel Fade No Overshoot";
    private const string BUTTON_OPTION = "Sidebar/Button";
    private const string BUTTON_NO_OVERSHOOT_OPTION = "Sidebar/Button No Overshoot";
    private const string BUTTON_SOFT_OPTION = "Sidebar/Button Soft";
    private const string BUTTON_SOFT_NO_OVERSHOOT_OPTION = "Sidebar/Button Soft No Overshoot";

    [SerializeField] private UISidebarEdgeDirection hiddenDirection = UISidebarEdgeDirection.Left;
    [SerializeField] private float extraHideOffset = 0f;
    [SerializeField] private bool useEnterOvershoot = true;
    [SerializeField] [Min(0f)] private float enterOvershootDistance = 36f;
    [SerializeField] [Range(0f, 1f)] private float enterOvershootDurationRatio = 0.78f;
    [SerializeField] private Ease enterOvershootEase = Ease.OutCubic;
    [SerializeField] private Ease enterSettleEase = Ease.OutCubic;

    private void Reset()
    {
        ApplyConfigByString(PANEL_OPTION);
    }

    public override List<string> GetOptionList()
    {
        List<string> options = base.GetOptionList();
        options.Add(PANEL_OPTION);
        options.Add(PANEL_NO_OVERSHOOT_OPTION);
        options.Add(PANEL_SOFT_OPTION);
        options.Add(PANEL_SOFT_NO_OVERSHOOT_OPTION);
        options.Add(PANEL_FADE_OPTION);
        options.Add(PANEL_FADE_NO_OVERSHOOT_OPTION);
        options.Add(BUTTON_OPTION);
        options.Add(BUTTON_NO_OVERSHOOT_OPTION);
        options.Add(BUTTON_SOFT_OPTION);
        options.Add(BUTTON_SOFT_NO_OVERSHOOT_OPTION);
        return options;
    }

    public override void ApplyConfigByString(string selectedOption)
    {
        if (!IsSidebarOption(selectedOption))
        {
            base.ApplyConfigByString(selectedOption);
            return;
        }

        if (UIMotionPresetResolver.TryGetPreset(selectedOption, out UISidebarMotionPreset sidebarPreset))
        {
            ApplyPreset(sidebarPreset);
            SetCurrentConfigOption(selectedOption);
            return;
        }

        Debug.LogWarning($"{GetType().Name} could not resolve UI sidebar motion preset option '{selectedOption}'.", this);
        ClearPresetReference();
        SetCurrentConfigOption(CUSTOM_OPTION);
    }

    /// <summary>侧边栏入场播放。</summary>
    public override Tween PlayEnter(float delay = 0f)
    {
        if (!useEnterOvershoot)
        {
            return base.PlayEnter(delay);
        }

        PrepareForPlay();
        UIMotionClip clip = GetClip(UIMotionAction.Show) ?? new UIMotionClip { action = UIMotionAction.Show };
        float overshootDuration = clip.duration * enterOvershootDurationRatio;
        float settleDuration = Mathf.Max(0f, clip.duration - overshootDuration);
        UIMotionPose overshootPose = CreateEnterOvershootPose();

        Sequence sequence = DOTween.Sequence().SetUpdate(UseUnscaledTime).SetDelay(delay);
        sequence.Append(TweenToPose(overshootPose, overshootDuration, enterOvershootEase, 0f, null));
        if (settleDuration > 0f)
        {
            sequence.Append(TweenToPose(new UIMotionPose(), settleDuration, enterSettleEase, 0f, null));
        }

        sequence.OnComplete(() => { });
        return RegisterTween(sequence);
    }

    /// <summary>立即设为隐藏状态。</summary>
    public override void SetHiddenImmediate() => SetImmediate(UIMotionAction.Hide);

    public void ConfigureSidebar(UISidebarEdgeDirection direction, float extraOffset = 0f)
    {
        hiddenDirection = direction;
        extraHideOffset = extraOffset;
    }

    public void ConfigureTimings(float showDuration, Ease showEase, float hideDuration, Ease hideEase)
    {
        SetClipTiming(UIMotionAction.Show, showDuration, showEase);
        SetClipTiming(UIMotionAction.Common, showDuration, showEase);
        SetClipTiming(UIMotionAction.Hide, hideDuration, hideEase);
    }

    protected override UIMotionPose GetPose(UIMotionAction action)
    {
        if (action == UIMotionAction.Hide)
        {
            UIMotionClip clip = GetClip(action);
            return CreateSidebarPose(clip?.pose.fade ?? false, clip?.pose.alpha ?? 1f);
        }

        return base.GetPose(action);
    }

    protected override void OnPresetApplied(UIMotionPreset motionPreset, string option)
    {
        if (motionPreset is not UISidebarMotionPreset sidebarPreset)
        {
            return;
        }

        ApplySidebarPreset(sidebarPreset);
    }

    private void ApplySidebarPreset(UISidebarMotionPreset sidebarPreset)
    {
        if (sidebarPreset.OverrideHiddenDirection)
        {
            hiddenDirection = sidebarPreset.HiddenDirection;
        }

        if (sidebarPreset.OverrideExtraHideOffset)
        {
            extraHideOffset = sidebarPreset.ExtraHideOffset;
        }

        useEnterOvershoot = sidebarPreset.UseEnterOvershoot;
        enterOvershootDistance = sidebarPreset.EnterOvershootDistance;
        enterOvershootDurationRatio = sidebarPreset.EnterOvershootDurationRatio;
        enterOvershootEase = sidebarPreset.EnterOvershootEase;
        enterSettleEase = sidebarPreset.EnterSettleEase;
    }

    private UIMotionPose CreateSidebarPose(bool fade, float alpha)
    {
        float width = TargetRect.rect.width > 0f ? TargetRect.rect.width : Mathf.Abs(TargetRect.sizeDelta.x);
        float height = TargetRect.rect.height > 0f ? TargetRect.rect.height : Mathf.Abs(TargetRect.sizeDelta.y);
        float distance = ((hiddenDirection == UISidebarEdgeDirection.Left || hiddenDirection == UISidebarEdgeDirection.Right) ? width : height) + extraHideOffset;
        Vector2 dir = hiddenDirection switch
        {
            UISidebarEdgeDirection.Right => Vector2.right,
            UISidebarEdgeDirection.Top => Vector2.up,
            UISidebarEdgeDirection.Bottom => Vector2.down,
            _ => Vector2.left
        };

        return new UIMotionPose { fade = fade, alpha = alpha, move = true, offset = dir * distance };
    }

    private UIMotionPose CreateEnterOvershootPose()
    {
        Vector2 direction = hiddenDirection switch
        {
            UISidebarEdgeDirection.Right => Vector2.left,
            UISidebarEdgeDirection.Top => Vector2.down,
            UISidebarEdgeDirection.Bottom => Vector2.up,
            _ => Vector2.right
        };

        return new UIMotionPose
        {
            move = true,
            offset = direction * enterOvershootDistance
        };
    }

    private void SetClipTiming(UIMotionAction action, float duration, Ease ease)
    {
        UIMotionClip clip = GetClip(action);
        if (clip == null)
        {
            Debug.LogWarning($"{GetType().Name} missing motion clip for action '{action}'.", this);
            return;
        }

        clip.duration = duration;
        clip.ease = ease;
    }

    private static bool IsSidebarOption(string selectedOption)
    {
        return !string.IsNullOrWhiteSpace(selectedOption)
               && selectedOption.StartsWith("Sidebar/", System.StringComparison.Ordinal);
    }

}
