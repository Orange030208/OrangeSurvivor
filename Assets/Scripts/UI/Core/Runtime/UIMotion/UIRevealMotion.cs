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
public abstract class UIRevealMotion : MonoBehaviour, IUISequenceMotion, IUIRuntimeMotion
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
    private bool highlightShown;
    private bool pressShown;

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
        Prepare(UIMotionAction.Show);
    }

    public virtual Tween PlayEnter(float delay = 0f)
    {
        return Play(UIMotionAction.Show, delay);
    }

    public virtual Tween PlayExit(float delay = 0f)
    {
        return Play(UIMotionAction.Hide, delay);
    }

    public virtual void SetHiddenImmediate()
    {
        SetImmediate(UIMotionAction.Show);
    }

    /// <summary>
    /// 统一的高层语义入口：
    /// - Show：进入正常展示态
    /// - Hide：进入隐藏态
    /// - Emphasis：播放一次性强调反馈
    /// - Highlight：进入高亮态
    /// - Press：进入按下态；结束后会回到高亮态或普通展示态
    /// </summary>
    public Tween Play(UIMotionAction action, float delay = 0f)
    {
        return action switch
        {
            UIMotionAction.Show => PlayToShown(action, delay),
            UIMotionAction.Hide => PlayToPose(action, delay),
            UIMotionAction.Emphasis => PlaySpecial(action, delay),
            UIMotionAction.Highlight => PlayToHighlight(delay),
            UIMotionAction.Press => PlayToPress(delay),
            _ => null
        };
    }

    public void SetImmediate(UIMotionAction action)
    {
        if (action == UIMotionAction.Show)
        {
            Prepare(action);
            return;
        }

        if (action == UIMotionAction.Hide)
        {
            PrepareForPlay();
            ApplyPoseImmediate(GetActionPose(action));
            if (ShouldDeactivateOnComplete(action))
            {
                gameObject.SetActive(false);
            }

            return;
        }

        if (action == UIMotionAction.Highlight)
        {
            PrepareForPlay();
            ApplyPoseImmediate(GetHighlightPose());
            highlightShown = true;
            pressShown = false;
            return;
        }

        if (action == UIMotionAction.Press)
        {
            PrepareForPlay();
            ApplyPoseImmediate(GetPressPose());
            pressShown = true;
            return;
        }

        CompleteImmediate();
    }

    public void CompleteImmediate()
    {
        PrepareForPlay();
        highlightShown = false;
        pressShown = false;
        ApplyShownImmediate();
    }

    public void RefreshDefaults()
    {
        EnsureReferences();
        cached = false;
        CacheDefaults();
    }

    public void Kill()
    {
        currentTween?.Kill();
        currentTween = null;
    }

    protected abstract UIMotionClip GetClip(UIMotionAction action);

    protected virtual UIMotionPose GetPreparePose(UIMotionAction action) => GetClip(action).pose;
    protected virtual UIMotionPose GetActionPose(UIMotionAction action) => GetClip(action).pose;
    protected virtual bool ShouldDeactivateOnComplete(UIMotionAction action) => GetClip(action).deactivateOnComplete;
    protected virtual Tween PlaySpecial(UIMotionAction action, float delay) => null;
    protected virtual UIMotionPose GetHighlightPose() => GetClip(UIMotionAction.Highlight).pose;
    protected virtual UIMotionPose GetPressPose() => GetClip(UIMotionAction.Press).pose;

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

    protected void ApplyShownImmediate()
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

    protected Tween TweenToShown(float duration, Ease ease, float delay, Action onCompleted)
    {
        Sequence sequence = DOTween.Sequence().SetUpdate(useUnscaledTime).SetDelay(delay);
        sequence.Join(canvasGroup.DOFade(1f, duration));
        sequence.Join(targetRect.DOAnchorPos(defaultAnchoredPosition, duration));
        sequence.Join(targetRect.DOScale(defaultScale, duration));
        sequence.Join(targetRect.DOLocalRotate(defaultEulerAngles, duration));
        sequence.SetEase(ease).OnComplete(() => onCompleted?.Invoke());
        return sequence;
    }

    protected Tween TweenToPose(UIMotionPose pose, float duration, Ease ease, float delay, Action onCompleted)
    {
        Sequence sequence = DOTween.Sequence().SetUpdate(useUnscaledTime).SetDelay(delay);
        if (pose.fade) sequence.Join(canvasGroup.DOFade(pose.alpha, duration));
        if (pose.move) sequence.Join(targetRect.DOAnchorPos(defaultAnchoredPosition + ScaleOffset(pose.offset), duration));
        if (pose.scale) sequence.Join(targetRect.DOScale(defaultScale * ScaleValue(pose.scaleMultiplier), duration));
        if (pose.rotate) sequence.Join(targetRect.DOLocalRotate(defaultEulerAngles + new Vector3(0f, 0f, ScaleRotation(pose.rotationZ)), duration));
        sequence.SetEase(ease).OnComplete(() => onCompleted?.Invoke());
        return sequence;
    }

    private void Prepare(UIMotionAction action)
    {
        PrepareForPlay();
        if (action == UIMotionAction.Show)
        {
            ApplyPoseImmediate(GetPreparePose(action));
            return;
        }

        if (action == UIMotionAction.Hide)
        {
            ApplyPoseImmediate(GetActionPose(action));
            return;
        }

        if (action == UIMotionAction.Highlight)
        {
            ApplyPoseImmediate(GetHighlightPose());
            highlightShown = true;
            return;
        }

        if (action == UIMotionAction.Press)
        {
            ApplyPoseImmediate(GetPressPose());
            pressShown = true;
            return;
        }

        ApplyShownImmediate();
    }

    private Tween PlayToShown(UIMotionAction action, float delay)
    {
        PrepareForPlay();
        highlightShown = false;
        pressShown = false;
        UIMotionClip clip = GetClip(action);
        currentTween = TweenToShown(clip.duration, clip.ease, delay, RestoreInteractionState);
        return currentTween;
    }

    private Tween PlayToPose(UIMotionAction action, float delay)
    {
        PrepareForPlay();
        highlightShown = false;
        pressShown = false;
        UIMotionClip clip = GetClip(action);
        currentTween = TweenToPose(GetActionPose(action), clip.duration, clip.ease, delay, () =>
        {
            if (ShouldDeactivateOnComplete(action))
            {
                gameObject.SetActive(false);
            }
        });
        return currentTween;
    }

    private Tween PlayToHighlight(float delay)
    {
        PrepareForPlay();
        pressShown = false;
        UIMotionClip clip = GetClip(UIMotionAction.Highlight);
        currentTween = TweenToPose(GetHighlightPose(), clip.duration, clip.ease, delay, () =>
        {
            highlightShown = true;
            RestoreInteractionState();
        });
        return currentTween;
    }

    private Tween PlayToPress(float delay)
    {
        PrepareForPlay();
        UIMotionClip clip = GetClip(UIMotionAction.Press);
        currentTween = TweenToPose(GetPressPose(), clip.duration, clip.ease, delay, () =>
        {
            pressShown = true;
            RestoreInteractionState();
        });
        return currentTween;
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
