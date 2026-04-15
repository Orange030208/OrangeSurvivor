using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 专用于局部强化表现：默认不负责显隐切换，只负责强调 / pulse 类动画。
/// </summary>
public class UIEmphasisOnlyMotion : UIRevealMotion
{
    public enum EmphasisMode { ScalePulse, PunchScale }

    [SerializeField] private bool enableEmphasis = true;
    [SerializeField] private EmphasisMode emphasisMode = EmphasisMode.ScalePulse;
    [SerializeField] private Vector3 emphasisPunch = new(0.12f, 0.12f, 0f);
    [SerializeField] private int emphasisVibrato = 8;
    [SerializeField] [Range(0f, 1f)] private float emphasisElasticity = 0.8f;
    [SerializeField] private List<UIMotionClip> actionClips = new()
    {
        new UIMotionClip { action = UIMotionAction.Show },
        new UIMotionClip { action = UIMotionAction.Hide },
        new UIMotionClip { action = UIMotionAction.Emphasis, pose = new UIMotionPose { scale = true, scaleMultiplier = 1.08f }, duration = 0.14f, ease = Ease.OutBack }
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

    /// <summary>播放局部强化动画。</summary>
    public Tween PlayEmphasis(float delay = 0f) => Play(UIMotionAction.Emphasis, delay);

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
            return null;
        }

        PrepareForPlay();
        if (!enableEmphasis)
        {
            CompleteImmediate();
            return null;
        }

        UIMotionClip clip = GetClip(UIMotionAction.Emphasis);
        Sequence sequence = DOTween.Sequence().SetUpdate(UseUnscaledTime).SetDelay(delay);
        if (emphasisMode == EmphasisMode.PunchScale)
        {
            sequence.Append(TargetRect.DOPunchScale(emphasisPunch * MotionIntensity, clip.duration, emphasisVibrato, emphasisElasticity));
        }
        else
        {
            sequence.Append(TargetRect.DOScale(DefaultScale * ScaleValue(clip.pose.scaleMultiplier), clip.duration).SetEase(clip.ease));
            sequence.Append(TargetRect.DOScale(DefaultScale, clip.duration).SetEase(Ease.OutCubic));
        }

        sequence.OnComplete(() =>
        {
            TargetRect.localScale = DefaultScale;
            RestoreInteractionState();
        });
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
