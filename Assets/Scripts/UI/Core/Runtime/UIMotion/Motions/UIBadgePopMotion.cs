using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 徽章提示专用：适合角标、掉落提示、小型提醒点。
/// </summary>
public class UIBadgePopMotion : UIEmphasisOnlyMotion
{
    [SerializeField] private List<UIMotionClip> actionClips = new()
    {
        new UIMotionClip { action = UIMotionAction.Show, pose = new UIMotionPose { fade = true, alpha = 0f, scale = true, scaleMultiplier = 0.72f }, duration = 0.12f, ease = Ease.OutBack },
        new UIMotionClip { action = UIMotionAction.Hide, pose = new UIMotionPose { fade = true, alpha = 0f, scale = true, scaleMultiplier = 0.82f }, duration = 0.08f, ease = Ease.InBack, deactivateOnComplete = true },
        new UIMotionClip { action = UIMotionAction.Emphasis, pose = new UIMotionPose { scale = true, scaleMultiplier = 1.14f }, duration = 0.1f, ease = Ease.OutBack },
        new UIMotionClip { action = UIMotionAction.Highlight, pose = new UIMotionPose { scale = true, scaleMultiplier = 1.08f }, duration = 0.08f, ease = Ease.OutQuad },
        new UIMotionClip { action = UIMotionAction.Press, pose = new UIMotionPose { scale = true, scaleMultiplier = 0.93f }, duration = 0.05f, ease = Ease.OutQuad }
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

    /// <summary>徽章入场预备。</summary>
    public override void PrepareEnter() => Play(UIMotionAction.Show);

    /// <summary>徽章入场播放。</summary>
    public override Tween PlayEnter(float delay = 0f) => Play(UIMotionAction.Show, delay);

    /// <summary>徽章退场播放。</summary>
    public override Tween PlayExit(float delay = 0f) => Play(UIMotionAction.Hide, delay);

    /// <summary>徽章显示。</summary>
    public  Tween Show(float delay = 0f) => Play(UIMotionAction.Show, delay);

    /// <summary>徽章隐藏。</summary>
    public Tween Hide(float delay = 0f) => Play(UIMotionAction.Hide, delay);

    /// <summary>徽章弹跳强化。</summary>
    public new Tween PlayEmphasis(float delay = 0f) => Play(UIMotionAction.Emphasis, delay);

    /// <summary>徽章高亮。</summary>
    public Tween PlayHighlight(float delay = 0f) => Play(UIMotionAction.Highlight, delay);

    /// <summary>徽章按下。</summary>
    public Tween PlayPress(float delay = 0f) => Play(UIMotionAction.Press, delay);

    /// <summary>立即设为入场前状态。</summary>
    public override void SetHiddenImmediate() => SetImmediate(UIMotionAction.Show);

    /// <summary>立即设为隐藏状态。</summary>
    public void SetExitImmediate() => SetImmediate(UIMotionAction.Hide);

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
