using System;
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
public abstract class UIRevealMotion : UIRuntimeMotionBase, IUISequenceMotion
{
    [SerializeField] [Min(0f)] private float motionIntensity = 1f;
    [SerializeField] private bool useUnscaledTime = true;

    private CanvasGroup canvasGroup;
    private RectTransform targetRect;
    private Tween currentTween;
    private Vector2 defaultAnchoredPosition;
    private Vector3 defaultScale;
    private Vector3 defaultEulerAngles;
    private bool cached;
    private bool interactionCached;
    private bool defaultInteractable;
    private bool defaultBlocksRaycasts;
    private UIMotionAction currentStableAction = UIMotionAction.Normal;

    protected CanvasGroup CanvasGroup => canvasGroup;
    protected RectTransform TargetRect => targetRect;
    protected Vector2 DefaultAnchoredPosition => defaultAnchoredPosition;
    protected Vector3 DefaultScale => defaultScale;
    protected Vector3 DefaultEulerAngles => defaultEulerAngles;
    protected float MotionIntensity => motionIntensity;
    protected bool UseUnscaledTime => useUnscaledTime;

    protected virtual void Awake()
    {
        EnsureReferences();
        CacheInteractionDefaults();
    }

    protected virtual void OnDestroy() => Kill();

    public virtual void PrepareEnter()
    {
        SetImmediate(UIMotionAction.Hide);
    }

    public virtual Tween PlayEnter(float delay = 0f)
    {
        return Play(UIMotionAction.Normal, delay);
    }

    public virtual Tween PlayExit(float delay = 0f)
    {
        return Play(UIMotionAction.Hide, delay);
    }

    public virtual void SetHiddenImmediate()
    {
        SetImmediate(UIMotionAction.Hide);
    }

    /// <summary>
    /// 统一的高层语义入口：
    /// - Normal：进入正常展示态
    /// - Hide：进入隐藏态
    /// - Emphasis：播放一次性强调反馈，结束后回到当前稳定态
    /// - Highlight：进入高亮态
    /// - Press：进入按下态
    /// 所有状态型动作都从“当前状态”过渡到目标状态，而不是先回默认态。
    /// </summary>
    public override Tween Play(UIMotionAction action, float delay = 0f)
    {
        return action switch
        {
            UIMotionAction.Normal => PlayToAction(action, delay),
            UIMotionAction.Hide => PlayToAction(action, delay),
            UIMotionAction.Highlight => SupportsAction(UIMotionAction.Highlight) ? PlayToAction(action, delay) : null,
            UIMotionAction.Press => SupportsAction(UIMotionAction.Press) ? PlayToAction(action, delay) : null,
            UIMotionAction.Emphasis => SupportsAction(UIMotionAction.Emphasis) ? PlaySpecial(action, delay) : null,
            _ => null
        };
    }

    public override void SetImmediate(UIMotionAction action)
    {
        PrepareForPlay();
        if (action == UIMotionAction.Emphasis)
        {
            ApplyCurrentStableStateImmediate();
            return;
        }

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
            UIMotionAction.Normal => true,
            UIMotionAction.Hide => true,
            UIMotionAction.Highlight => HasClip(UIMotionAction.Highlight),
            UIMotionAction.Press => HasClip(UIMotionAction.Press),
            UIMotionAction.Emphasis => HasClip(UIMotionAction.Emphasis),
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

    protected abstract UIMotionClip GetClip(UIMotionAction action);

    // 扩展说明：子类可覆盖以提供动态目标姿态，例如侧边栏根据尺寸计算隐藏位。
    protected virtual UIMotionPose GetPose(UIMotionAction action) => GetClip(action).pose;
    protected virtual bool ShouldDeactivateOnComplete(UIMotionAction action) => GetClip(action).deactivateOnComplete;
    protected virtual Tween PlaySpecial(UIMotionAction action, float delay) => null;

    protected void PrepareForPlay()
    {
        EnsureReferences();
        CacheDefaults();
        CacheInteractionDefaults();
        Kill();
        gameObject.SetActive(true);
        RestoreInteractionState();
    }

    protected void RestoreInteractionState()
    {
        canvasGroup.interactable = defaultInteractable;
        canvasGroup.blocksRaycasts = defaultBlocksRaycasts;
    }

    protected Vector2 ScaleOffset(Vector2 offset) => offset * motionIntensity;
    protected float ScaleValue(float value) => 1f + ((value - 1f) * motionIntensity);
    protected float ScaleRotation(float zRotation) => zRotation * motionIntensity;

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
        targetRect.localScale = pose.scale ? defaultScale * ScaleValue(pose.scaleMultiplier) : defaultScale;
        targetRect.localEulerAngles = pose.rotate ? defaultEulerAngles + new Vector3(0f, 0f, ScaleRotation(pose.rotationZ)) : defaultEulerAngles;
    }

    protected Tween TweenToPose(UIMotionPose pose, float duration, Ease ease, float delay, Action onCompleted)
    {
        Sequence sequence = DOTween.Sequence().SetUpdate(useUnscaledTime).SetDelay(delay);
        if (pose.fade) sequence.Join(canvasGroup.DOFade(pose.alpha, duration));
        else sequence.Join(canvasGroup.DOFade(1f, duration));

        Vector2 targetPosition = pose.move ? defaultAnchoredPosition + ScaleOffset(pose.offset) : defaultAnchoredPosition;
        Vector3 targetScale = pose.scale ? defaultScale * ScaleValue(pose.scaleMultiplier) : defaultScale;
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
        if (ShouldDeactivateOnComplete(action))
        {
            gameObject.SetActive(false);
        }
    }

    protected void ApplyCurrentStableStateImmediate()
    {
        UIMotionAction stableAction = currentStableAction;
        if (stableAction == UIMotionAction.Emphasis)
        {
            stableAction = UIMotionAction.Normal;
        }

        UIMotionPose pose = ResolvePose(stableAction);
        ApplyPoseImmediate(pose);
    }

    protected Tween PlayToAction(UIMotionAction action, float delay)
    {
        PrepareForPlay();
        UIMotionClip clip = GetClip(action);
        UIMotionPose pose = ResolvePose(action);
        currentTween = TweenToPose(pose, clip.duration, clip.ease, delay, () =>
        {
            UpdateStableAction(action);
            if (ShouldDeactivateOnComplete(action))
            {
                gameObject.SetActive(false);
            }

            RestoreInteractionState();
        });
        return currentTween;
    }

    protected UIMotionPose ResolvePose(UIMotionAction action)
    {
        if (action == UIMotionAction.Normal)
        {
            return CreateDefaultPose();
        }

        return GetPose(action);
    }

    protected void UpdateStableAction(UIMotionAction action)
    {
        if (action != UIMotionAction.Emphasis)
        {
            currentStableAction = action;
        }
    }

    private UIMotionPose CreateDefaultPose()
    {
        return new UIMotionPose();
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

    private void CacheInteractionDefaults()
    {
        if (interactionCached) return;
        defaultInteractable = canvasGroup.interactable;
        defaultBlocksRaycasts = canvasGroup.blocksRaycasts;
        interactionCached = true;
    }
}
