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
        new UIMotionClip { action = UIMotionAction.Normal, duration = 0.24f, ease = Ease.OutCubic },
        new UIMotionClip { action = UIMotionAction.Hide, duration = 0.22f, ease = Ease.InCubic },
        new UIMotionClip { action = UIMotionAction.Emphasis, pose = new UIMotionPose { scale = true, scaleMultiplier = 1.02f }, duration = 0.1f, ease = Ease.OutQuad }
    };

    /// <summary>侧边栏显示。</summary>
    public Tween Show(float delay = 0f) => Play(UIMotionAction.Normal, delay);

    /// <summary>侧边栏隐藏。</summary>
    public Tween Hide(float delay = 0f) => Play(UIMotionAction.Hide, delay);

    /// <summary>侧边栏入场播放。</summary>
    public override Tween PlayEnter(float delay = 0f) => Play(UIMotionAction.Normal, delay);

    /// <summary>侧边栏退场播放。</summary>
    public override Tween PlayExit(float delay = 0f) => Play(UIMotionAction.Hide, delay);

    /// <summary>立即设为侧边栏隐藏位。</summary>
    public void SetExitImmediate() => SetImmediate(UIMotionAction.Hide);

    /// <summary>立即设为隐藏状态。</summary>
    public override void SetHiddenImmediate() => SetImmediate(UIMotionAction.Hide);

    public void ConfigureSidebar(EdgeDirection direction, float extraOffset = 0f, bool inactiveOnHide = true)
    {
        hiddenDirection = direction;
        extraHideOffset = extraOffset;
        setInactiveOnHide = inactiveOnHide;
    }

    public void ConfigureTimings(float showDuration, Ease showEase, float hideDuration, Ease hideEase)
    {
        SetClipTiming(UIMotionAction.Normal, showDuration, showEase);
        SetClipTiming(UIMotionAction.Hide, hideDuration, hideEase);
    }

    protected override UIMotionClip GetClip(UIMotionAction action)
    {
        for (int i = 0; i < actionClips.Count; i++)
        {
            UIMotionClip clip = actionClips[i];
            if (clip != null && clip.action == action)
            {
                return clip;
            }
        }

        return new UIMotionClip { action = action };
    }

    // 扩展说明：侧边栏的隐藏态由当前尺寸和边缘方向动态计算，Normal 始终回到默认布局位。
    protected override UIMotionPose GetPose(UIMotionAction action)
    {
        if (action == UIMotionAction.Hide)
        {
            UIMotionClip clip = GetClip(action);
            return CreateSidebarPose(clip.pose.fade, clip.pose.alpha);
        }

        return base.GetPose(action);
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

    private UIMotionClip FindClip(UIMotionAction action)
    {
        for (int i = 0; i < actionClips.Count; i++)
        {
            UIMotionClip clip = actionClips[i];
            if (clip != null && clip.action == action)
            {
                return clip;
            }
        }

        return null;
    }

    private void SetClipTiming(UIMotionAction action, float duration, Ease ease)
    {
        UIMotionClip clip = FindClip(action);
        if (clip == null)
        {
            Debug.LogWarning($"{GetType().Name} missing motion clip for action '{action}'.", this);
            return;
        }

        clip.duration = duration;
        clip.ease = ease;
    }
}
