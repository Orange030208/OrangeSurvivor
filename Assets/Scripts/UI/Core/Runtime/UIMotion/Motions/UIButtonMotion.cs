using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 按钮/列表项专用动效：只控制显影、缩放与旋转，不改布局位置。
/// 适合 click-only 交互目标、ScrollView 子项、受 LayoutGroup / ContentSizeFitter 驱动的 UI。
/// </summary>
public class UIButtonMotion : UIRevealMotion
{
    private static readonly Vector2 HIGHLIGHT_OFFSET = new(0f, 2f);
    private static readonly Vector2 PRESS_OFFSET = new(0f, -4f);

    [SerializeField] private List<UIMotionClip> actionClips = new()
    {
        new UIMotionClip
        {
            action = UIMotionAction.Normal,
            pose = new UIMotionPose(),
            duration = 0.12f,
            ease = Ease.OutQuad
        },
        new UIMotionClip
        {
            action = UIMotionAction.Hide,
            pose = new UIMotionPose
            {
                fade = true,
                alpha = 0f,
                move = false,
                offset = Vector2.zero,
                scale = true,
                scaleMultiplier = 0.96f
            },
            duration = 0.14f,
            ease = Ease.InCubic
        },
        new UIMotionClip
        {
            action = UIMotionAction.Emphasis,
            pose = new UIMotionPose { scale = true, scaleMultiplier = 1.1f },
            duration = 0.16f,
            ease = Ease.OutBack
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
        }
    };

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

    protected override Tween PlaySpecial(UIMotionAction action, float delay)
    {
        if (action != UIMotionAction.Emphasis)
        {
            return base.PlaySpecial(action, delay);
        }

        PrepareForPlay();
        UIMotionClip clip = GetClip(action);
        float halfDuration = Mathf.Max(0.01f, clip.duration * 0.5f);

        // 扩展说明：强调反馈总是从当前稳定态出发，播放后再回到当前稳定态。
        UIMotionPose currentPose = ResolvePose(UIMotionAction.Normal);
        Sequence sequence = DOTween.Sequence().SetUpdate(UseUnscaledTime).SetDelay(delay);
        sequence.Append(TweenToPose(clip.pose, halfDuration, clip.ease, 0f, null));
        sequence.Append(TweenToPose(currentPose, halfDuration, Ease.InOutQuad, 0f, RestoreInteractionState));
        return sequence;
    }
}
