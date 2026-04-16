using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 结合 Sidebar 入退场与 Button 交互反馈的动效：
/// - 显隐阶段使用侧边栏式边缘滑入滑出
/// - 交互阶段支持 Highlight / Press / Emphasis
/// 适合可点击的抽屉标签、侧栏入口按钮、浮出式侧边操作项。
/// </summary>
public class UISidebarButtonMotion : UIRevealMotion
{
    public enum EdgeDirection { Left, Right, Top, Bottom }

    private static readonly Vector2 HIGHLIGHT_OFFSET = new(0f, 2f);
    private static readonly Vector2 PRESS_OFFSET = new(0f, -4f);

    [SerializeField] private EdgeDirection hiddenDirection = EdgeDirection.Left;
    [SerializeField] private float extraHideOffset = 0f;
    [SerializeField] private bool setInactiveOnHide = true;
    [SerializeField] private List<UIMotionClip> actionClips = new()
    {
        new UIMotionClip
        {
            action = UIMotionAction.Normal,
            pose = new UIMotionPose(),
            duration = 0.24f,
            ease = Ease.OutCubic
        },
        new UIMotionClip
        {
            action = UIMotionAction.Hide,
            pose = new UIMotionPose
            {
                fade = true,
                alpha = 0f,
                move = true
            },
            duration = 0.22f,
            ease = Ease.InCubic
        },
        new UIMotionClip
        {
            action = UIMotionAction.Highlight,
            pose = new UIMotionPose
            {
                move = true,
                offset = HIGHLIGHT_OFFSET,
                scale = true,
                scaleMultiplier = 1.02f
            },
            duration = 0.08f,
            ease = Ease.OutQuad
        },
        new UIMotionClip
        {
            action = UIMotionAction.Press,
            pose = new UIMotionPose
            {
                move = true,
                offset = PRESS_OFFSET,
                scale = true,
                scaleMultiplier = 0.96f
            },
            duration = 0.08f,
            ease = Ease.OutQuad
        },
        new UIMotionClip
        {
            action = UIMotionAction.Emphasis,
            pose = new UIMotionPose
            {
                scale = true,
                scaleMultiplier = 1.08f
            },
            duration = 0.14f,
            ease = Ease.OutBack
        }
    };

    public Tween Show(float delay = 0f) => Play(UIMotionAction.Normal, delay);

    public Tween Hide(float delay = 0f) => Play(UIMotionAction.Hide, delay);

    public Tween PlayHighlight(float delay = 0f) => Play(UIMotionAction.Highlight, delay);

    public Tween PlayPress(float delay = 0f) => Play(UIMotionAction.Press, delay);

    public Tween PlayEmphasis(float delay = 0f) => Play(UIMotionAction.Emphasis, delay);

    public override Tween PlayEnter(float delay = 0f) => Play(UIMotionAction.Normal, delay);

    public override Tween PlayExit(float delay = 0f) => Play(UIMotionAction.Hide, delay);

    public override void SetHiddenImmediate() => SetImmediate(UIMotionAction.Hide);

    public void ConfigureSidebar(EdgeDirection direction, float extraOffset = 0f, bool inactiveOnHide = true)
    {
        hiddenDirection = direction;
        extraHideOffset = extraOffset;
        setInactiveOnHide = inactiveOnHide;
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

        Debug.LogWarning($"{GetType().Name} missing motion clip for action '{action}'.", this);
        return new UIMotionClip { action = action };
    }

    // 扩展说明：Hide 姿态沿侧边栏方向动态计算，其余交互态保持按钮式局部反馈。
    protected override UIMotionPose GetPose(UIMotionAction action)
    {
        if (action != UIMotionAction.Hide)
        {
            return base.GetPose(action);
        }

        UIMotionClip clip = GetClip(action);
        return CreateSidebarHidePose(clip.pose.fade, clip.pose.alpha);
    }

    protected override bool ShouldDeactivateOnComplete(UIMotionAction action)
    {
        return base.ShouldDeactivateOnComplete(action) || (action == UIMotionAction.Hide && setInactiveOnHide);
    }

    protected override Tween PlaySpecial(UIMotionAction action, float delay)
    {
        if (action != UIMotionAction.Emphasis)
        {
            return base.PlaySpecial(action, delay);
        }

        PrepareForPlay();
        UIMotionClip clip = GetClip(action);
        float halfDuration = Mathf.Max(0.01f, clip.duration * 0.5f);

        // 扩展说明：强调反馈从当前稳定态短暂放大后回落，不改变当前显隐/交互目标态。
        UIMotionPose currentPose = ResolveCurrentStablePose();
        Sequence sequence = DOTween.Sequence().SetUpdate(UseUnscaledTime).SetDelay(delay);
        sequence.Append(TweenToPose(clip.pose, halfDuration, clip.ease, 0f, null));
        sequence.Append(TweenToPose(currentPose, halfDuration, Ease.InOutQuad, 0f, RestoreInteractionState));
        return sequence;
    }

    private UIMotionPose CreateSidebarHidePose(bool fade, float alpha)
    {
        float width = TargetRect.rect.width > 0f ? TargetRect.rect.width : Mathf.Abs(TargetRect.sizeDelta.x);
        float height = TargetRect.rect.height > 0f ? TargetRect.rect.height : Mathf.Abs(TargetRect.sizeDelta.y);
        bool horizontal = hiddenDirection == EdgeDirection.Left || hiddenDirection == EdgeDirection.Right;
        float distance = (horizontal ? width : height) + extraHideOffset;
        Vector2 direction = hiddenDirection switch
        {
            EdgeDirection.Right => Vector2.right,
            EdgeDirection.Top => Vector2.up,
            EdgeDirection.Bottom => Vector2.down,
            _ => Vector2.left
        };

        return new UIMotionPose
        {
            fade = fade,
            alpha = alpha,
            move = true,
            offset = direction * distance
        };
    }

    private UIMotionPose ResolveCurrentStablePose()
    {
        if (CanvasGroup.alpha <= 0f)
        {
            return GetPose(UIMotionAction.Hide);
        }

        return new UIMotionPose();
    }
}
