using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 轻提示专用：适合 tooltip、悬浮说明、小型气泡信息。
/// </summary>
public class UITooltipRevealMotion : UIRevealMotion
{
    [SerializeField] private List<UIMotionClip> actionClips = new()
    {
        new UIMotionClip { action = UIMotionAction.Normal, pose = new UIMotionPose(), duration = 0.12f, ease = Ease.OutQuad },
        new UIMotionClip { action = UIMotionAction.Hide, pose = new UIMotionPose { fade = true, alpha = 0f, move = true, offset = new Vector2(0f, 6f), scale = true, scaleMultiplier = 0.98f }, duration = 0.1f, ease = Ease.InQuad, deactivateOnComplete = true },
        new UIMotionClip { action = UIMotionAction.Emphasis, pose = new UIMotionPose { scale = true, scaleMultiplier = 1.03f }, duration = 0.08f, ease = Ease.OutQuad },
        new UIMotionClip { action = UIMotionAction.Highlight, pose = new UIMotionPose { move = true, offset = new Vector2(0f, 2f), scale = true, scaleMultiplier = 1.02f }, duration = 0.08f, ease = Ease.OutQuad },
        new UIMotionClip { action = UIMotionAction.Press, pose = new UIMotionPose { scale = true, scaleMultiplier = 0.98f }, duration = 0.05f, ease = Ease.OutQuad }
    };

    /// <summary>Tooltip 入场预备。</summary>
    public new void PrepareEnter() => SetImmediate(UIMotionAction.Hide);

    /// <summary>Tooltip 入场播放。</summary>
    public new Tween PlayEnter(float delay = 0f) => Play(UIMotionAction.Normal, delay);

    /// <summary>Tooltip 退场播放。</summary>
    public new Tween PlayExit(float delay = 0f) => Play(UIMotionAction.Hide, delay);

    /// <summary>Tooltip 显示。</summary>
    public Tween Show(float delay = 0f) => Play(UIMotionAction.Normal, delay);

    /// <summary>Tooltip 隐藏。</summary>
    public Tween Hide(float delay = 0f) => Play(UIMotionAction.Hide, delay);

    /// <summary>Tooltip 轻微强调。</summary>
    public Tween PlayEmphasis(float delay = 0f) => Play(UIMotionAction.Emphasis, delay);

    /// <summary>Tooltip 高亮。</summary>
    public Tween PlayHighlight(float delay = 0f) => Play(UIMotionAction.Highlight, delay);

    /// <summary>Tooltip 按下。</summary>
    public Tween PlayPress(float delay = 0f) => Play(UIMotionAction.Press, delay);

    /// <summary>立即设为隐藏状态。</summary>
    public new void SetHiddenImmediate() => SetImmediate(UIMotionAction.Hide);

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
}
