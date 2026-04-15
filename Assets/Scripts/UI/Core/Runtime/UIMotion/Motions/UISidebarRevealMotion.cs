using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 专用于侧边栏/抽屉类 UI：负责从边缘滑入滑出，并可选附带淡出。
/// </summary>
public class UISidebarRevealMotion : UIRevealMotion
{
    public enum EdgeDirection { Left, Right, Top, Bottom }

    [SerializeField] private EdgeDirection hiddenDirection = EdgeDirection.Left;
    [SerializeField] private float extraHideOffset = 0f;
    [SerializeField] private bool setInactiveOnHide = true;
    [SerializeField] private List<UIMotionClip> actionClips = new()
    {
        new UIMotionClip { action = UIMotionAction.Show, duration = 0.24f, ease = Ease.OutCubic },
        new UIMotionClip { action = UIMotionAction.Hide, duration = 0.22f, ease = Ease.InCubic },
        new UIMotionClip { action = UIMotionAction.Emphasis, pose = new UIMotionPose { scale = true, scaleMultiplier = 1.02f }, duration = 0.1f, ease = Ease.OutQuad }
    };

    private readonly Dictionary<UIMotionAction, UIMotionClip> clipMap = new();

    protected override void Awake()
    {
        RebuildClipMap();
        base.Awake();
    }

    private void OnValidate()
    {
        RebuildClipMap();
    }

    /// <summary>侧边栏显示。</summary>
    public Tween Show(float delay = 0f) => Play(UIMotionAction.Show, delay);

    /// <summary>侧边栏隐藏。</summary>
    public Tween Hide(float delay = 0f) => Play(UIMotionAction.Hide, delay);

    /// <summary>侧边栏入场播放。</summary>
    public override Tween PlayEnter(float delay = 0f) => Play(UIMotionAction.Show, delay);

    /// <summary>侧边栏退场播放。</summary>
    public override Tween PlayExit(float delay = 0f) => Play(UIMotionAction.Hide, delay);

    /// <summary>立即设为侧边栏隐藏位。</summary>
    public void SetExitImmediate() => SetImmediate(UIMotionAction.Hide);

    /// <summary>立即设为入场前隐藏位。</summary>
    public override void SetHiddenImmediate() => SetImmediate(UIMotionAction.Show);

    public void ConfigureSidebar(EdgeDirection direction, float extraOffset = 0f, bool inactiveOnHide = true)
    {
        hiddenDirection = direction;
        extraHideOffset = extraOffset;
        setInactiveOnHide = inactiveOnHide;
    }

    public void ConfigureTimings(float showDuration, Ease showEase, float hideDuration, Ease hideEase)
    {
        SetClipTiming(UIMotionAction.Show, showDuration, showEase);
        SetClipTiming(UIMotionAction.Hide, hideDuration, hideEase);
    }

    protected override UIMotionClip GetClip(UIMotionAction action)
    {
        if (clipMap.TryGetValue(action, out UIMotionClip clip))
        {
            return clip;
        }

        UIMotionClip created = new() { action = action };
        actionClips.Add(created);
        clipMap[action] = created;
        return created;
    }

    protected override UIMotionPose GetPreparePose(UIMotionAction action)
    {
        return CreateSidebarPose(false, 1f);
    }

    protected override UIMotionPose GetActionPose(UIMotionAction action)
    {
        UIMotionClip clip = GetClip(action);
        if (action == UIMotionAction.Hide)
        {
            return CreateSidebarPose(clip.pose.fade, clip.pose.alpha);
        }

        return clip.pose;
    }

    protected override bool ShouldDeactivateOnComplete(UIMotionAction action)
    {
        return base.ShouldDeactivateOnComplete(action) || (action == UIMotionAction.Hide && setInactiveOnHide);
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

    private void RebuildClipMap()
    {
        clipMap.Clear();
        foreach (UIMotionClip clip in actionClips)
        {
            if (clip != null)
            {
                clipMap[clip.action] = clip;
            }
        }
    }

    private void SetClipTiming(UIMotionAction action, float duration, Ease ease)
    {
        UIMotionClip clip = GetClip(action);
        clip.duration = duration;
        clip.ease = ease;
    }
}
