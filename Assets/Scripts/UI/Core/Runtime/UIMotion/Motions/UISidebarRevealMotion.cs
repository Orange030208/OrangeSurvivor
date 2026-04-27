using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 专用于侧边栏/抽屉类 UI：负责从边缘滑入滑出，并可选附带淡出。
/// </summary>
public class UISidebarRevealMotion : UIRevealMotion
{
    public enum EdgeDirection { Left, Right, Top, Bottom }

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

    [SerializeField] private EdgeDirection hiddenDirection = EdgeDirection.Left;
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
        ClearPresetReference();
        switch (selectedOption)
        {
            case PANEL_OPTION:
                actionClips = CreatePanelSidebarClips();
                useEnterOvershoot = true;
                SetCurrentConfigOption(selectedOption);
                return;
            case PANEL_NO_OVERSHOOT_OPTION:
                actionClips = CreatePanelSidebarClips();
                useEnterOvershoot = false;
                SetCurrentConfigOption(selectedOption);
                return;
            case PANEL_SOFT_OPTION:
                actionClips = CreatePanelSoftSidebarClips();
                useEnterOvershoot = true;
                SetCurrentConfigOption(selectedOption);
                return;
            case PANEL_SOFT_NO_OVERSHOOT_OPTION:
                actionClips = CreatePanelSoftSidebarClips();
                useEnterOvershoot = false;
                SetCurrentConfigOption(selectedOption);
                return;
            case PANEL_FADE_OPTION:
                actionClips = CreatePanelFadeSidebarClips();
                useEnterOvershoot = true;
                SetCurrentConfigOption(selectedOption);
                return;
            case PANEL_FADE_NO_OVERSHOOT_OPTION:
                actionClips = CreatePanelFadeSidebarClips();
                useEnterOvershoot = false;
                SetCurrentConfigOption(selectedOption);
                return;
            case BUTTON_OPTION:
                actionClips = CreateButtonSidebarClips();
                useEnterOvershoot = true;
                SetCurrentConfigOption(selectedOption);
                return;
            case BUTTON_NO_OVERSHOOT_OPTION:
                actionClips = CreateButtonSidebarClips();
                useEnterOvershoot = false;
                SetCurrentConfigOption(selectedOption);
                return;
            case BUTTON_SOFT_OPTION:
                actionClips = CreateButtonSoftSidebarClips();
                useEnterOvershoot = true;
                SetCurrentConfigOption(selectedOption);
                return;
            case BUTTON_SOFT_NO_OVERSHOOT_OPTION:
                actionClips = CreateButtonSoftSidebarClips();
                useEnterOvershoot = false;
                SetCurrentConfigOption(selectedOption);
                return;
            default:
                base.ApplyConfigByString(selectedOption);
                return;
        }
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

    public void ConfigureSidebar(EdgeDirection direction, float extraOffset = 0f)
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

    private UIMotionPose CreateSidebarPose(bool fade, float alpha)
    {
        float width = TargetRect.rect.width > 0f ? TargetRect.rect.width : Mathf.Abs(TargetRect.sizeDelta.x);
        float height = TargetRect.rect.height > 0f ? TargetRect.rect.height : Mathf.Abs(TargetRect.sizeDelta.y);
        float distance = ((hiddenDirection == EdgeDirection.Left || hiddenDirection == EdgeDirection.Right) ? width : height) + extraHideOffset;
        Vector2 dir = hiddenDirection switch
        {
            EdgeDirection.Right => Vector2.right,
            EdgeDirection.Top => Vector2.up,
            EdgeDirection.Bottom => Vector2.down,
            _ => Vector2.left
        };

        return new UIMotionPose { fade = fade, alpha = alpha, move = true, offset = dir * distance };
    }

    private UIMotionPose CreateEnterOvershootPose()
    {
        Vector2 direction = hiddenDirection switch
        {
            EdgeDirection.Right => Vector2.left,
            EdgeDirection.Top => Vector2.down,
            EdgeDirection.Bottom => Vector2.up,
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

    private static List<UIMotionClip> CreatePanelSidebarClips()
    {
        return new List<UIMotionClip>
        {
            new() { action = UIMotionAction.Common, pose = new UIMotionPose(), duration = 0.22f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Show, pose = new UIMotionPose(), duration = 0.22f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Hide, pose = new UIMotionPose { fade = true, alpha = 0f, move = true }, duration = 0.22f, ease = Ease.InCubic }
        };
    }

    private static List<UIMotionClip> CreatePanelSoftSidebarClips()
    {
        return new List<UIMotionClip>
        {
            new() { action = UIMotionAction.Common, pose = new UIMotionPose(), duration = 0.28f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Show, pose = new UIMotionPose(), duration = 0.28f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Hide, pose = new UIMotionPose { fade = true, alpha = 0f, move = true }, duration = 0.2f, ease = Ease.InQuad }
        };
    }

    private static List<UIMotionClip> CreatePanelFadeSidebarClips()
    {
        return new List<UIMotionClip>
        {
            new() { action = UIMotionAction.Common, pose = new UIMotionPose(), duration = 0.2f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Show, pose = new UIMotionPose { fade = true, alpha = 1f }, duration = 0.2f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Hide, pose = new UIMotionPose { fade = true, alpha = 0f, move = true }, duration = 0.18f, ease = Ease.InCubic }
        };
    }

    private static List<UIMotionClip> CreateButtonSidebarClips()
    {
        return new List<UIMotionClip>
        {
            new() { action = UIMotionAction.Common, pose = new UIMotionPose(), duration = 0.22f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Show, pose = new UIMotionPose(), duration = 0.22f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Hide, pose = new UIMotionPose { fade = true, alpha = 0f, move = true }, duration = 0.22f, ease = Ease.InCubic },
            new() { action = UIMotionAction.Enter, pose = new UIMotionPose { move = true, offset = new Vector2(0f, 2f), scale = true, scaleMultiplier = 1.02f }, duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Exit, pose = new UIMotionPose(), duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Press, pose = new UIMotionPose { move = true, offset = new Vector2(0f, -4f), scale = true, scaleMultiplier = 0.96f }, duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Emphasis, pose = new UIMotionPose { scale = true, scaleMultiplier = 1.1f }, duration = 0.16f, ease = Ease.OutBack }
        };
    }

    private static List<UIMotionClip> CreateButtonSoftSidebarClips()
    {
        return new List<UIMotionClip>
        {
            new() { action = UIMotionAction.Common, pose = new UIMotionPose(), duration = 0.24f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Show, pose = new UIMotionPose(), duration = 0.24f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Hide, pose = new UIMotionPose { fade = true, alpha = 0f, move = true }, duration = 0.2f, ease = Ease.InQuad },
            new() { action = UIMotionAction.Enter, pose = new UIMotionPose { move = true, offset = new Vector2(0f, 1f), scale = true, scaleMultiplier = 1.01f }, duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Exit, pose = new UIMotionPose(), duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Press, pose = new UIMotionPose { move = true, offset = new Vector2(0f, -2f), scale = true, scaleMultiplier = 0.98f }, duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Emphasis, pose = new UIMotionPose { scale = true, scaleMultiplier = 1.06f }, duration = 0.14f, ease = Ease.OutBack }
        };
    }
}
