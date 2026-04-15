using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 按钮/列表项专用动效：只控制显影、缩放与旋转，不改布局位置。
/// 适合 click-only 交互目标、ScrollView 子项、受 LayoutGroup / ContentSizeFitter 驱动的 UI。
/// </summary>
public class UIButtonMotion : UIRevealMotion
{
    [SerializeField] private List<UIMotionClip> actionClips = new()
    {
        new UIMotionClip
        {
            action = UIMotionAction.Show,
            pose = new UIMotionPose
            {
                fade = true,
                alpha = 0f,
                move = false,
                offset = Vector2.zero,
                scale = true,
                scaleMultiplier = 0.94f
            },
            duration = 0.18f,
            ease = Ease.OutCubic
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
            pose = new UIMotionPose { scale = true, scaleMultiplier = 1.04f },
            duration = 0.12f,
            ease = Ease.OutQuad
        },
        new UIMotionClip
        {
            action = UIMotionAction.Press,
            pose = new UIMotionPose { scale = true, scaleMultiplier = 0.97f },
            duration = 0.06f,
            ease = Ease.OutQuad
        }
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

    protected override Tween PlaySpecial(UIMotionAction action, float delay)
    {
        if (action != UIMotionAction.Emphasis)
        {
            return base.PlaySpecial(action, delay);
        }

        PrepareForPlay();
        UIMotionClip clip = GetClip(action);
        float halfDuration = Mathf.Max(0.01f, clip.duration * 0.5f);
        Sequence sequence = DOTween.Sequence().SetUpdate(UseUnscaledTime).SetDelay(delay);
        sequence.Append(TweenToPose(clip.pose, halfDuration, clip.ease, 0f, null));
        sequence.Append(TweenToShown(halfDuration, Ease.InOutQuad, 0f, RestoreInteractionState));
        return sequence;
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
}
