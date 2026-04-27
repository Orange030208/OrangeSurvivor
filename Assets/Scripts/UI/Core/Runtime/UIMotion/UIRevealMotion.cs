using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[Serializable]
public class UIMotionPose
{
    public bool fade;
    [Range(0f, 1f)] public float alpha = 1f;
    public bool move;
    public Vector2 offset;
    public bool scale;
    public float scaleMultiplier = 1f;
    public bool scaleX;
    public float scaleXMultiplier = 1f;
    public bool scaleY;
    public float scaleYMultiplier = 1f;
    public bool rotate;
    public float rotationZ;
}

[Serializable]
public class UIMotionClip
{
    public UIMotionAction action;
    public UIMotionPose pose = new();
    public float duration = 0.2f;
    public Ease ease = Ease.OutCubic;
    public bool deactivateOnComplete;
}

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(RectTransform))]
public class UIRevealMotion : UIRuntimeMotionBase, IUISequenceMotion
{
    protected const string CUSTOM_OPTION = "Custom";
    protected const string DEFAULT_OPTION = "Default";
    protected const string DEFAULT_SOFT_OPTION = "Default/Soft";
    protected const string DEFAULT_FADE_OPTION = "Default/Fade";
    protected const string DEFAULT_CRISP_OPTION = "Default/Crisp";
    protected const string BUTTON_OPTION = "Button";
    protected const string BUTTON_SOFT_OPTION = "Button/Soft";
    protected const string BUTTON_FADE_OPTION = "Button/Fade";
    protected const string BUTTON_CRISP_OPTION = "Button/Crisp";
    protected const string COLLAPSE_X_OPTION = "Button/CollapseX";
    protected const string BADGE_OPTION = "Badge";
    protected const string BADGE_SOFT_OPTION = "Badge/Soft";
    protected const string BADGE_CRISP_OPTION = "Badge/Crisp";
    protected const string TOOLTIP_OPTION = "Tooltip";
    protected const string TOOLTIP_SOFT_OPTION = "Tooltip/Soft";
    protected const string TOOLTIP_FADE_OPTION = "Tooltip/Fade";
    protected const string TOOLTIP_CRISP_OPTION = "Tooltip/Crisp";

    [SerializeField] [Min(0f)] private float motionIntensity = 1f;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private UIMotionPreset preset;
    [SerializeField] protected List<UIMotionClip> actionClips = new();

    private CanvasGroup canvasGroup;
    private RectTransform targetRect;
    private Tween currentTween;
    private Vector2 defaultAnchoredPosition;
    private Vector3 defaultScale;
    private Vector3 defaultEulerAngles;
    private bool cached;
    private UIMotionAction currentStableAction = UIMotionAction.Common;

    protected CanvasGroup CanvasGroup => canvasGroup;
    protected RectTransform TargetRect => targetRect;
    protected Vector2 DefaultAnchoredPosition => defaultAnchoredPosition;
    protected Vector3 DefaultScale => defaultScale;
    protected Vector3 DefaultEulerAngles => defaultEulerAngles;
    protected float MotionIntensity => motionIntensity;
    protected bool UseUnscaledTime => useUnscaledTime;

    protected virtual void Awake()
    {
        ApplySerializedPresetIfNeeded();
        EnsureReferences();
    }

    protected virtual void OnValidate()
    {
        ApplySerializedPresetIfNeeded();
    }

    protected virtual void OnDestroy() => Kill();

    public override List<string> GetOptionList()
    {
        return new List<string>
        {
            CUSTOM_OPTION,
            DEFAULT_OPTION,
            DEFAULT_SOFT_OPTION,
            DEFAULT_FADE_OPTION,
            DEFAULT_CRISP_OPTION,
            BUTTON_OPTION,
            BUTTON_SOFT_OPTION,
            BUTTON_FADE_OPTION,
            BUTTON_CRISP_OPTION,
            COLLAPSE_X_OPTION,
            BADGE_OPTION,
            BADGE_SOFT_OPTION,
            BADGE_CRISP_OPTION,
            TOOLTIP_OPTION,
            TOOLTIP_SOFT_OPTION,
            TOOLTIP_FADE_OPTION,
            TOOLTIP_CRISP_OPTION
        };
    }

    public override void ApplyConfigByString(string selectedOption)
    {
        ClearPresetReference();
        if (string.IsNullOrWhiteSpace(selectedOption))
        {
            SetCurrentConfigOption(CUSTOM_OPTION);
            return;
        }

        switch (selectedOption)
        {
            case DEFAULT_OPTION:
                actionClips = CreateDefaultPresetClips();
                break;
            case DEFAULT_SOFT_OPTION:
                actionClips = CreateDefaultSoftPresetClips();
                break;
            case DEFAULT_FADE_OPTION:
                actionClips = CreateDefaultFadePresetClips();
                break;
            case DEFAULT_CRISP_OPTION:
                actionClips = CreateDefaultCrispPresetClips();
                break;
            case BUTTON_OPTION:
                actionClips = CreateButtonPresetClips();
                break;
            case BUTTON_SOFT_OPTION:
                actionClips = CreateButtonSoftPresetClips();
                break;
            case BUTTON_FADE_OPTION:
                actionClips = CreateButtonFadePresetClips();
                break;
            case BUTTON_CRISP_OPTION:
                actionClips = CreateButtonCrispPresetClips();
                break;
            case COLLAPSE_X_OPTION:
                actionClips = CreateCollapseXPresetClips();
                break;
            case BADGE_OPTION:
                actionClips = CreateBadgePresetClips();
                break;
            case BADGE_SOFT_OPTION:
                actionClips = CreateBadgeSoftPresetClips();
                break;
            case BADGE_CRISP_OPTION:
                actionClips = CreateBadgeCrispPresetClips();
                break;
            case TOOLTIP_OPTION:
                actionClips = CreateTooltipPresetClips();
                break;
            case TOOLTIP_SOFT_OPTION:
                actionClips = CreateTooltipSoftPresetClips();
                break;
            case TOOLTIP_FADE_OPTION:
                actionClips = CreateTooltipFadePresetClips();
                break;
            case TOOLTIP_CRISP_OPTION:
                actionClips = CreateTooltipCrispPresetClips();
                break;
            default:
                selectedOption = CUSTOM_OPTION;
                break;
        }

        SetCurrentConfigOption(selectedOption);
    }

    public void ApplyPreset(UIMotionPreset motionPreset)
    {
        preset = motionPreset;
        ApplySerializedPresetIfNeeded();
    }

    private void ApplySerializedPresetIfNeeded()
    {
        if (preset == null)
        {
            return;
        }

        useUnscaledTime = preset.UseUnscaledTime;
        actionClips = preset.CreateRuntimeClips();
        SetCurrentConfigOption(preset.name);
    }

    protected void ClearPresetReference()
    {
        preset = null;
    }

    public virtual void PrepareEnter()
    {
        SetImmediate(UIMotionAction.Hide);
    }

    public virtual Tween PlayEnter(float delay = 0f)
    {
        return Play(UIMotionAction.Show, delay);
    }

    public virtual Tween PlayExit(float delay = 0f)
    {
        return Play(UIMotionAction.Hide, delay);
    }

    public override Tween PlayVisibility(UIVisibilityMotion motion, float delay = 0f)
    {
        return Play(UIMotionActionMapper.ToLegacyAction(motion), delay);
    }

    public virtual void SetHiddenImmediate()
    {
        SetImmediate(UIMotionAction.Hide);
    }

    public override void SetVisibilityImmediate(UIVisibilityMotion motion)
    {
        SetImmediate(UIMotionActionMapper.ToLegacyAction(motion));
    }

    /// <summary>
    /// 统一的高层语义入口：
    /// - Show：执行展示/入场语义，对应 reveal、展开、显现等首次可见动作
    /// - Common：回到稳定常态/默认态，不等同于入场播放
    /// - Hide：进入隐藏/退场语义
    /// - Press：进入按下态
    /// - Release：进入松开回弹态
    /// - Emphasis：先迅速放大，再回到当前稳定态
    /// - Enter：进入鼠标悬停态
    /// - Exit：离开鼠标悬停态
    /// 所有状态型动作都从“当前状态”过渡到目标状态，而不是先回默认态。
    /// </summary>
    public override Tween Play(UIMotionAction action, float delay = 0f)
    {
        return action switch
        {
            UIMotionAction.Show => PlayToAction(action, delay),
            UIMotionAction.Common => PlayToAction(action, delay),
            UIMotionAction.Hide => PlayToAction(action, delay),
            UIMotionAction.Press => SupportsAction(UIMotionAction.Press) ? PlayToAction(action, delay) : null,
            UIMotionAction.Release => SupportsAction(UIMotionAction.Release) ? PlayToAction(action, delay) : null,
            UIMotionAction.Emphasis => SupportsAction(UIMotionAction.Emphasis) ? PlayEmphasis(delay) : null,
            UIMotionAction.Enter => SupportsAction(UIMotionAction.Enter) ? PlayToAction(action, delay) : null,
            UIMotionAction.Exit => SupportsAction(UIMotionAction.Exit) ? PlayToAction(action, delay) : null,
            _ => null
        };
    }

    public override void SetImmediate(UIMotionAction action)
    {
        PrepareForPlay();
        ApplyActionImmediate(action);
    }

    public void CompleteImmediate()
    {
        PrepareForPlay();
        ApplyCurrentStableStateImmediate();
    }

    public override void RefreshDefaults()
    {
        EnsureReferences();
        cached = false;
        CacheDefaults();
    }

    public override void Kill()
    {
        currentTween?.Kill();
        currentTween = null;
    }

    public override bool SupportsAction(UIMotionAction action)
    {
        return action switch
        {
            UIMotionAction.Show => true,
            UIMotionAction.Common => true,
            UIMotionAction.Hide => true,
            UIMotionAction.Press => HasClip(UIMotionAction.Press),
            UIMotionAction.Release => HasClip(UIMotionAction.Release),
            UIMotionAction.Emphasis => HasClip(UIMotionAction.Emphasis),
            UIMotionAction.Enter => HasClip(UIMotionAction.Enter),
            UIMotionAction.Exit => HasClip(UIMotionAction.Exit),
            _ => false
        };
    }

    protected bool HasClip(UIMotionAction action)
    {
        return TryGetClip(action, out _);
    }

    protected bool TryGetClip(UIMotionAction action, out UIMotionClip clip)
    {
        clip = GetClip(action);
        return clip != null;
    }

    protected void AddClip(UIMotionClip clip)
    {
        if (clip == null)
        {
            return;
        }

        actionClips.Add(clip);
    }

    protected virtual UIMotionClip GetClip(UIMotionAction action)
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

    // 扩展说明：子类可覆盖以提供动态目标姿态，例如侧边栏根据尺寸计算隐藏位。
    protected virtual UIMotionPose GetPose(UIMotionAction action) => GetClip(action)?.pose ?? new UIMotionPose();

    protected void PrepareForPlay()
    {
        EnsureReferences();
        CacheDefaults();
        Kill();
    }

    protected Vector2 ScaleOffset(Vector2 offset) => offset * motionIntensity;
    protected float ScaleValue(float value) => 1f + ((value - 1f) * motionIntensity);
    protected float ScaleRotation(float zRotation) => zRotation * motionIntensity;

    protected Vector3 ResolveTargetScale(UIMotionPose pose)
    {
        Vector3 targetScale = pose.scale ? defaultScale * ScaleValue(pose.scaleMultiplier) : defaultScale;
        if (pose.scaleX)
        {
            targetScale.x = defaultScale.x * ScaleValue(pose.scaleXMultiplier);
        }

        if (pose.scaleY)
        {
            targetScale.y = defaultScale.y * ScaleValue(pose.scaleYMultiplier);
        }

        return targetScale;
    }

    protected void ApplyDefaultStateImmediate()
    {
        canvasGroup.alpha = 1f;
        targetRect.anchoredPosition = defaultAnchoredPosition;
        targetRect.localScale = defaultScale;
        targetRect.localEulerAngles = defaultEulerAngles;
    }

    protected void ApplyPoseImmediate(UIMotionPose pose)
    {
        canvasGroup.alpha = pose.fade ? pose.alpha : 1f;
        targetRect.anchoredPosition = pose.move ? defaultAnchoredPosition + ScaleOffset(pose.offset) : defaultAnchoredPosition;
        targetRect.localScale = ResolveTargetScale(pose);
        targetRect.localEulerAngles = pose.rotate ? defaultEulerAngles + new Vector3(0f, 0f, ScaleRotation(pose.rotationZ)) : defaultEulerAngles;
    }

    protected Tween RegisterTween(Tween tween)
    {
        currentTween = tween;
        return tween;
    }

    protected Tween TweenToPose(UIMotionPose pose, float duration, Ease ease, float delay, Action onCompleted)
    {
        Sequence sequence = DOTween.Sequence().SetUpdate(useUnscaledTime).SetDelay(delay);
        if (pose.fade) sequence.Join(canvasGroup.DOFade(pose.alpha, duration));
        else sequence.Join(canvasGroup.DOFade(1f, duration));

        Vector2 targetPosition = pose.move ? defaultAnchoredPosition + ScaleOffset(pose.offset) : defaultAnchoredPosition;
        Vector3 targetScale = ResolveTargetScale(pose);
        Vector3 targetRotation = pose.rotate ? defaultEulerAngles + new Vector3(0f, 0f, ScaleRotation(pose.rotationZ)) : defaultEulerAngles;

        sequence.Join(targetRect.DOAnchorPos(targetPosition, duration));
        sequence.Join(targetRect.DOScale(targetScale, duration));
        sequence.Join(targetRect.DOLocalRotate(targetRotation, duration));
        sequence.SetEase(ease).OnComplete(() => onCompleted?.Invoke());
        return sequence;
    }

    protected void ApplyActionImmediate(UIMotionAction action)
    {
        UIMotionPose pose = ResolvePose(action);
        ApplyPoseImmediate(pose);
        UpdateStableAction(action);

        UIMotionClip clip = GetClip(action);
        if (clip != null && clip.deactivateOnComplete)
        {
            Debug.LogWarning($"{GetType().Name} '{name}' ignores deactivateOnComplete. Page activation is owned by UIPageBase.", this);
        }
    }

    protected void ApplyCurrentStableStateImmediate()
    {
        UIMotionAction stableAction = currentStableAction;
        if (stableAction == UIMotionAction.Emphasis)
        {
            stableAction = UIMotionAction.Common;
        }

        UIMotionPose pose = ResolvePose(stableAction);
        ApplyPoseImmediate(pose);
    }

    protected Tween PlayEmphasis(float delay)
    {
        PrepareForPlay();
        UIMotionClip clip = GetClip(UIMotionAction.Emphasis) ?? new UIMotionClip { action = UIMotionAction.Emphasis };
        float expandDuration = clip.duration * 0.55f;
        float settleDuration = Mathf.Max(0f, clip.duration - expandDuration);
        UIMotionPose stablePose = ResolvePose(currentStableAction == UIMotionAction.Emphasis ? UIMotionAction.Common : currentStableAction);

        Sequence sequence = DOTween.Sequence().SetUpdate(useUnscaledTime).SetDelay(delay);
        sequence.Append(TweenToPose(clip.pose, expandDuration, clip.ease, 0f, null));
        if (settleDuration > 0f)
        {
            sequence.Append(TweenToPose(stablePose, settleDuration, Ease.OutQuad, 0f, null));
        }

        sequence.OnComplete(() =>
        {
            ApplyCurrentStableStateImmediate();
        });

        currentTween = RegisterTween(sequence);
        return currentTween;
    }

    protected Tween PlayToAction(UIMotionAction action, float delay)
    {
        PrepareForPlay();
        UIMotionClip clip = GetClip(action) ?? new UIMotionClip { action = action };
        UIMotionPose pose = ResolvePose(action);
        currentTween = RegisterTween(TweenToPose(pose, clip.duration, clip.ease, delay, () =>
        {
            UpdateStableAction(action);
            if (clip.deactivateOnComplete)
            {
                Debug.LogWarning($"{GetType().Name} '{name}' ignores deactivateOnComplete. Page activation is owned by UIPageBase.", this);
            }
        }));
        return currentTween;
    }

    protected UIMotionPose ResolvePose(UIMotionAction action)
    {
        if (action == UIMotionAction.Show && !HasClip(UIMotionAction.Show))
        {
            return CreateDefaultPose();
        }

        if (action == UIMotionAction.Common && !HasClip(UIMotionAction.Common))
        {
            return CreateDefaultPose();
        }

        return GetPose(action);
    }

    protected void UpdateStableAction(UIMotionAction action)
    {
        currentStableAction = action;
    }

    private UIMotionPose CreateDefaultPose()
    {
        return new UIMotionPose();
    }

    private static List<UIMotionClip> CreateDefaultPresetClips()
    {
        return new List<UIMotionClip>
        {
            new() { action = UIMotionAction.Common, pose = new UIMotionPose(), duration = 0.18f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Show, pose = new UIMotionPose(), duration = 0.18f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Hide, pose = new UIMotionPose { fade = true, alpha = 0f, move = true, offset = new Vector2(0f, -18f), scale = true, scaleMultiplier = 0.96f }, duration = 0.18f, ease = Ease.InCubic },
            new() { action = UIMotionAction.Enter, pose = new UIMotionPose { move = true, offset = new Vector2(0f, 4f), scale = true, scaleMultiplier = 1.03f }, duration = 0.12f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Exit, pose = new UIMotionPose(), duration = 0.1f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Emphasis, pose = new UIMotionPose { scale = true, scaleMultiplier = 1.12f }, duration = 0.16f, ease = Ease.OutBack }
        };
    }

    private static List<UIMotionClip> CreateDefaultSoftPresetClips()
    {
        return new List<UIMotionClip>
        {
            new() { action = UIMotionAction.Common, pose = new UIMotionPose(), duration = 0.24f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Show, pose = new UIMotionPose(), duration = 0.24f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Hide, pose = new UIMotionPose { fade = true, alpha = 0f, move = true, offset = new Vector2(0f, -12f), scale = true, scaleMultiplier = 0.98f }, duration = 0.18f, ease = Ease.InQuad },
            new() { action = UIMotionAction.Enter, pose = new UIMotionPose { move = true, offset = new Vector2(0f, 2f), scale = true, scaleMultiplier = 1.015f }, duration = 0.1f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Exit, pose = new UIMotionPose(), duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Emphasis, pose = new UIMotionPose { scale = true, scaleMultiplier = 1.08f }, duration = 0.14f, ease = Ease.OutQuad }
        };
    }

    private static List<UIMotionClip> CreateDefaultFadePresetClips()
    {
        return new List<UIMotionClip>
        {
            new() { action = UIMotionAction.Common, pose = new UIMotionPose(), duration = 0.18f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Show, pose = new UIMotionPose { fade = true, alpha = 1f, move = true, offset = new Vector2(0f, -8f) }, duration = 0.18f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Hide, pose = new UIMotionPose { fade = true, alpha = 0f, move = true, offset = new Vector2(0f, -8f), scale = true, scaleMultiplier = 0.99f }, duration = 0.14f, ease = Ease.InQuad },
            new() { action = UIMotionAction.Enter, pose = new UIMotionPose { scale = true, scaleMultiplier = 1.02f }, duration = 0.1f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Exit, pose = new UIMotionPose(), duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Emphasis, pose = new UIMotionPose { fade = true, alpha = 1f, scale = true, scaleMultiplier = 1.1f }, duration = 0.14f, ease = Ease.OutBack }
        };
    }

    private static List<UIMotionClip> CreateDefaultCrispPresetClips()
    {
        return new List<UIMotionClip>
        {
            new() { action = UIMotionAction.Common, pose = new UIMotionPose(), duration = 0.12f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Show, pose = new UIMotionPose(), duration = 0.12f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Hide, pose = new UIMotionPose { fade = true, alpha = 0f, move = true, offset = new Vector2(0f, -24f), scale = true, scaleMultiplier = 0.94f }, duration = 0.1f, ease = Ease.InCubic },
            new() { action = UIMotionAction.Enter, pose = new UIMotionPose { move = true, offset = new Vector2(0f, 5f), scale = true, scaleMultiplier = 1.04f }, duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Exit, pose = new UIMotionPose(), duration = 0.06f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Emphasis, pose = new UIMotionPose { scale = true, scaleMultiplier = 1.14f }, duration = 0.12f, ease = Ease.OutBack }
        };
    }

    private static List<UIMotionClip> CreateButtonPresetClips()
    {
        return new List<UIMotionClip>
        {
            new() { action = UIMotionAction.Common, pose = new UIMotionPose(), duration = 0.14f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Show, pose = new UIMotionPose(), duration = 0.14f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Hide, pose = new UIMotionPose { fade = true, alpha = 0f, scale = true, scaleMultiplier = 0.96f }, duration = 0.14f, ease = Ease.InCubic },
            new() { action = UIMotionAction.Enter, pose = new UIMotionPose { move = true, offset = new Vector2(0f, 2f), scale = true, scaleMultiplier = 1.02f }, duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Exit, pose = new UIMotionPose(), duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Release, pose = new UIMotionPose { move = true, offset = new Vector2(0f, 1f), scale = true, scaleMultiplier = 1.03f }, duration = 0.1f, ease = Ease.OutBack },
            new() { action = UIMotionAction.Press, pose = new UIMotionPose { move = true, offset = new Vector2(0f, -4f), scale = true, scaleMultiplier = 0.96f }, duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Emphasis, pose = new UIMotionPose { scale = true, scaleMultiplier = 1.16f }, duration = 0.18f, ease = Ease.OutBack }
        };
    }

    private static List<UIMotionClip> CreateButtonSoftPresetClips()
    {
        return new List<UIMotionClip>
        {
            new() { action = UIMotionAction.Common, pose = new UIMotionPose(), duration = 0.18f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Show, pose = new UIMotionPose(), duration = 0.18f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Hide, pose = new UIMotionPose { fade = true, alpha = 0f, scale = true, scaleMultiplier = 0.98f }, duration = 0.14f, ease = Ease.InQuad },
            new() { action = UIMotionAction.Enter, pose = new UIMotionPose { move = true, offset = new Vector2(0f, 1f), scale = true, scaleMultiplier = 1.01f }, duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Exit, pose = new UIMotionPose(), duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Release, pose = new UIMotionPose { move = true, offset = new Vector2(0f, 1f), scale = true, scaleMultiplier = 1.02f }, duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Press, pose = new UIMotionPose { move = true, offset = new Vector2(0f, -2f), scale = true, scaleMultiplier = 0.98f }, duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Emphasis, pose = new UIMotionPose { scale = true, scaleMultiplier = 1.08f }, duration = 0.14f, ease = Ease.OutQuad }
        };
    }

    private static List<UIMotionClip> CreateButtonFadePresetClips()
    {
        return new List<UIMotionClip>
        {
            new() { action = UIMotionAction.Common, pose = new UIMotionPose(), duration = 0.16f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Show, pose = new UIMotionPose { fade = true, alpha = 1f, scale = true, scaleMultiplier = 0.99f }, duration = 0.16f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Hide, pose = new UIMotionPose { fade = true, alpha = 0f, scale = true, scaleMultiplier = 0.95f }, duration = 0.12f, ease = Ease.InQuad },
            new() { action = UIMotionAction.Enter, pose = new UIMotionPose { scale = true, scaleMultiplier = 1.02f }, duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Exit, pose = new UIMotionPose(), duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Release, pose = new UIMotionPose { scale = true, scaleMultiplier = 1.02f }, duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Press, pose = new UIMotionPose { scale = true, scaleMultiplier = 0.97f }, duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Emphasis, pose = new UIMotionPose { fade = true, alpha = 1f, scale = true, scaleMultiplier = 1.12f }, duration = 0.14f, ease = Ease.OutBack }
        };
    }

    private static List<UIMotionClip> CreateButtonCrispPresetClips()
    {
        return new List<UIMotionClip>
        {
            new() { action = UIMotionAction.Common, pose = new UIMotionPose(), duration = 0.1f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Show, pose = new UIMotionPose(), duration = 0.1f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Hide, pose = new UIMotionPose { fade = true, alpha = 0f, scale = true, scaleMultiplier = 0.92f }, duration = 0.08f, ease = Ease.InCubic },
            new() { action = UIMotionAction.Enter, pose = new UIMotionPose { move = true, offset = new Vector2(0f, 3f), scale = true, scaleMultiplier = 1.03f }, duration = 0.06f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Exit, pose = new UIMotionPose(), duration = 0.06f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Release, pose = new UIMotionPose { move = true, offset = new Vector2(0f, 1f), scale = true, scaleMultiplier = 1.04f }, duration = 0.08f, ease = Ease.OutBack },
            new() { action = UIMotionAction.Press, pose = new UIMotionPose { move = true, offset = new Vector2(0f, -4f), scale = true, scaleMultiplier = 0.94f }, duration = 0.06f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Emphasis, pose = new UIMotionPose { scale = true, scaleMultiplier = 1.18f }, duration = 0.12f, ease = Ease.OutBack }
        };
    }

    private static List<UIMotionClip> CreateCollapseXPresetClips()
    {
        return new List<UIMotionClip>
        {
            new() { action = UIMotionAction.Common, pose = new UIMotionPose(), duration = 0.16f, ease = Ease.OutBack },
            new() { action = UIMotionAction.Show, pose = new UIMotionPose(), duration = 0.16f, ease = Ease.OutBack },
            new() { action = UIMotionAction.Hide, pose = new UIMotionPose { fade = true, alpha = 0f, scale = true, scaleMultiplier = 0.96f, scaleX = true, scaleXMultiplier = 0.4f }, duration = 0.14f, ease = Ease.InCubic, deactivateOnComplete = true },
            new() { action = UIMotionAction.Enter, pose = new UIMotionPose { move = true, offset = new Vector2(0f, 2f), scale = true, scaleMultiplier = 1.02f }, duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Exit, pose = new UIMotionPose(), duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Release, pose = new UIMotionPose { move = true, offset = new Vector2(0f, 1f), scale = true, scaleMultiplier = 1.03f }, duration = 0.1f, ease = Ease.OutBack },
            new() { action = UIMotionAction.Press, pose = new UIMotionPose { move = true, offset = new Vector2(0f, -4f), scale = true, scaleMultiplier = 0.96f }, duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Emphasis, pose = new UIMotionPose { scale = true, scaleMultiplier = 1.16f }, duration = 0.18f, ease = Ease.OutBack }
        };
    }

    private static List<UIMotionClip> CreateBadgePresetClips()
    {
        return new List<UIMotionClip>
        {
            new() { action = UIMotionAction.Common, pose = new UIMotionPose(), duration = 0.08f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Show, pose = new UIMotionPose(), duration = 0.08f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Hide, pose = new UIMotionPose { fade = true, alpha = 0f, scale = true, scaleMultiplier = 0.82f }, duration = 0.08f, ease = Ease.InBack, deactivateOnComplete = true },
            new() { action = UIMotionAction.Emphasis, pose = new UIMotionPose { scale = true, scaleMultiplier = 1.2f }, duration = 0.12f, ease = Ease.OutBack }
        };
    }

    private static List<UIMotionClip> CreateBadgeSoftPresetClips()
    {
        return new List<UIMotionClip>
        {
            new() { action = UIMotionAction.Common, pose = new UIMotionPose(), duration = 0.12f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Show, pose = new UIMotionPose(), duration = 0.12f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Hide, pose = new UIMotionPose { fade = true, alpha = 0f, scale = true, scaleMultiplier = 0.9f }, duration = 0.1f, ease = Ease.InQuad, deactivateOnComplete = true },
            new() { action = UIMotionAction.Emphasis, pose = new UIMotionPose { scale = true, scaleMultiplier = 1.12f }, duration = 0.12f, ease = Ease.OutQuad }
        };
    }

    private static List<UIMotionClip> CreateBadgeCrispPresetClips()
    {
        return new List<UIMotionClip>
        {
            new() { action = UIMotionAction.Common, pose = new UIMotionPose(), duration = 0.06f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Show, pose = new UIMotionPose(), duration = 0.06f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Hide, pose = new UIMotionPose { fade = true, alpha = 0f, scale = true, scaleMultiplier = 0.76f }, duration = 0.06f, ease = Ease.InBack, deactivateOnComplete = true },
            new() { action = UIMotionAction.Emphasis, pose = new UIMotionPose { scale = true, scaleMultiplier = 1.24f }, duration = 0.1f, ease = Ease.OutBack }
        };
    }

    private static List<UIMotionClip> CreateTooltipPresetClips()
    {
        return new List<UIMotionClip>
        {
            new() { action = UIMotionAction.Common, pose = new UIMotionPose(), duration = 0.1f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Show, pose = new UIMotionPose(), duration = 0.1f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Hide, pose = new UIMotionPose { fade = true, alpha = 0f, move = true, offset = new Vector2(0f, 6f), scale = true, scaleMultiplier = 0.98f }, duration = 0.1f, ease = Ease.InQuad, deactivateOnComplete = true },
            new() { action = UIMotionAction.Enter, pose = new UIMotionPose { move = true, offset = new Vector2(0f, 2f), scale = true, scaleMultiplier = 1.02f }, duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Exit, pose = new UIMotionPose(), duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Emphasis, pose = new UIMotionPose { scale = true, scaleMultiplier = 1.08f, move = true, offset = new Vector2(0f, 3f) }, duration = 0.1f, ease = Ease.OutBack }
        };
    }

    private static List<UIMotionClip> CreateTooltipSoftPresetClips()
    {
        return new List<UIMotionClip>
        {
            new() { action = UIMotionAction.Common, pose = new UIMotionPose(), duration = 0.14f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Show, pose = new UIMotionPose(), duration = 0.14f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Hide, pose = new UIMotionPose { fade = true, alpha = 0f, move = true, offset = new Vector2(0f, 4f), scale = true, scaleMultiplier = 0.99f }, duration = 0.12f, ease = Ease.InQuad, deactivateOnComplete = true },
            new() { action = UIMotionAction.Enter, pose = new UIMotionPose { move = true, offset = new Vector2(0f, 1f), scale = true, scaleMultiplier = 1.01f }, duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Exit, pose = new UIMotionPose(), duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Emphasis, pose = new UIMotionPose { scale = true, scaleMultiplier = 1.05f, move = true, offset = new Vector2(0f, 2f) }, duration = 0.12f, ease = Ease.OutQuad }
        };
    }

    private static List<UIMotionClip> CreateTooltipFadePresetClips()
    {
        return new List<UIMotionClip>
        {
            new() { action = UIMotionAction.Common, pose = new UIMotionPose(), duration = 0.12f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Show, pose = new UIMotionPose { fade = true, alpha = 1f, move = true, offset = new Vector2(0f, 4f) }, duration = 0.12f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Hide, pose = new UIMotionPose { fade = true, alpha = 0f, move = true, offset = new Vector2(0f, 4f), scale = true, scaleMultiplier = 0.99f }, duration = 0.1f, ease = Ease.InQuad, deactivateOnComplete = true },
            new() { action = UIMotionAction.Enter, pose = new UIMotionPose { scale = true, scaleMultiplier = 1.015f }, duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Exit, pose = new UIMotionPose(), duration = 0.08f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Emphasis, pose = new UIMotionPose { fade = true, alpha = 1f, scale = true, scaleMultiplier = 1.07f, move = true, offset = new Vector2(0f, 2f) }, duration = 0.1f, ease = Ease.OutBack }
        };
    }

    private static List<UIMotionClip> CreateTooltipCrispPresetClips()
    {
        return new List<UIMotionClip>
        {
            new() { action = UIMotionAction.Common, pose = new UIMotionPose(), duration = 0.08f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Show, pose = new UIMotionPose(), duration = 0.08f, ease = Ease.OutCubic },
            new() { action = UIMotionAction.Hide, pose = new UIMotionPose { fade = true, alpha = 0f, move = true, offset = new Vector2(0f, 8f), scale = true, scaleMultiplier = 0.97f }, duration = 0.08f, ease = Ease.InCubic, deactivateOnComplete = true },
            new() { action = UIMotionAction.Enter, pose = new UIMotionPose { move = true, offset = new Vector2(0f, 3f), scale = true, scaleMultiplier = 1.03f }, duration = 0.06f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Exit, pose = new UIMotionPose(), duration = 0.06f, ease = Ease.OutQuad },
            new() { action = UIMotionAction.Emphasis, pose = new UIMotionPose { scale = true, scaleMultiplier = 1.1f, move = true, offset = new Vector2(0f, 3f) }, duration = 0.08f, ease = Ease.OutBack }
        };
    }

    private void EnsureReferences()
    {
        targetRect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void CacheDefaults()
    {
        if (cached) return;
        defaultAnchoredPosition = targetRect.anchoredPosition;
        defaultScale = targetRect.localScale;
        defaultEulerAngles = targetRect.localEulerAngles;
        cached = true;
    }
}
