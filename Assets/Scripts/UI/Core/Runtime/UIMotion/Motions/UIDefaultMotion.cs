using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 通用默认动效：适合标题、按钮、卡片等常规 UI 元素。
/// 约定：
/// - Show：从“预备姿态”进入正常展示态
/// - Hide：从当前状态退出到隐藏姿态
/// - Highlight：进入 hover / 选中 / focus 的高亮态
/// - Press：进入按下态，抬起后通常回到 Show 或 Highlight
/// - Emphasis：播放一次性强调反馈
/// </summary>
public class UIDefaultMotion : UIRevealMotion
{
    [SerializeField] private List<UIMotionClip> actionClips = new()
    {
        new UIMotionClip
        {
            action = UIMotionAction.Show,
            pose = new UIMotionPose
            {
                fade = true, alpha = 0f, move = true, offset = new Vector2(0f, 32f), scale = true,
                scaleMultiplier = 0.94f
            },
            duration = 0.24f, ease = Ease.OutCubic
        },
        new UIMotionClip
        {
            action = UIMotionAction.Hide,
            pose = new UIMotionPose
            {
                fade = true, alpha = 0f, move = true, offset = new Vector2(0f, -18f), scale = true,
                scaleMultiplier = 0.96f
            },
            duration = 0.18f, ease = Ease.InCubic
        },
        new UIMotionClip
        {
            action = UIMotionAction.Emphasis,
            pose = new UIMotionPose { scale = true, scaleMultiplier = 1.06f },
            duration = 0.14f, ease = Ease.OutQuad
        },
        new UIMotionClip
        {
            action = UIMotionAction.Highlight,
            pose = new UIMotionPose { move = true, offset = new Vector2(0f, 4f), scale = true, scaleMultiplier = 1.03f },
            duration = 0.12f, ease = Ease.OutQuad
        },
        new UIMotionClip
        {
            action = UIMotionAction.Press,
            pose = new UIMotionPose { scale = true, scaleMultiplier = 0.97f },
            duration = 0.08f, ease = Ease.OutQuad
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

    /// <summary>常规元素入场预备。</summary>
    public void PrepareEnter() => Play(UIMotionAction.Show);

    /// <summary>常规元素入场播放。</summary>
    public Tween PlayEnter(float delay = 0f) => Play(UIMotionAction.Show, delay);

    /// <summary>常规元素退场播放。</summary>
    public Tween PlayExit(float delay = 0f) => Play(UIMotionAction.Hide, delay);

    /// <summary>常规元素显示。</summary>
    public Tween Show(float delay = 0f) => Play(UIMotionAction.Show, delay);

    /// <summary>常规元素隐藏。</summary>
    public Tween Hide(float delay = 0f) => Play(UIMotionAction.Hide, delay);

    /// <summary>常规元素 hover / 选中高亮。</summary>
    public Tween PlayHighlight(float delay = 0f) => Play(UIMotionAction.Highlight, delay);

    /// <summary>常规元素按下。</summary>
    public Tween PlayPress(float delay = 0f) => Play(UIMotionAction.Press, delay);

    /// <summary>常规元素点击强调。</summary>
    public Tween PlayEmphasis(float delay = 0f) => Play(UIMotionAction.Emphasis, delay);

    /// <summary>立即设为入场前状态。</summary>
    public void SetHiddenImmediate() => SetImmediate(UIMotionAction.Show);

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
