using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 文本打字机动效：通过控制 TMP 文本可见字符数，实现逐字显现/收起。
/// 适合标题、剧情短句、说明文案等需要按字符节奏展示的 UI 文本。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public class UITypewriterMotion : UIRuntimeMotionBase, IUISequenceMotion
{
    protected enum TypewriterDurationMode
    {
        FixedDuration,
        CharactersPerSecond
    }

    [Serializable]
    protected class TypewriterMotionClip
    {
        [Min(0.01f)] public float duration = 0.2f;
        public Ease ease = Ease.Linear;
        [Tooltip("Legacy compatibility only. Page activation is owned by UIPageBase.")]
        public bool deactivateOnComplete;
        public bool restartFromBeginning;
        public UIMotionAction action;
        public TypewriterDurationMode durationMode = TypewriterDurationMode.CharactersPerSecond;
        [Min(1f)] public float charactersPerSecond = 24f;
    }

    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private List<TypewriterMotionClip> actionClips = new()
    {
        new TypewriterMotionClip
        {
            action = UIMotionAction.Common,
            duration = 0.36f,
            ease = Ease.Linear,
            restartFromBeginning = false,
            durationMode = TypewriterDurationMode.CharactersPerSecond,
            charactersPerSecond = 24f
        },
        new TypewriterMotionClip
        {
            action = UIMotionAction.Show,
            duration = 0.36f,
            ease = Ease.Linear,
            restartFromBeginning = true,
            durationMode = TypewriterDurationMode.CharactersPerSecond,
            charactersPerSecond = 24f
        },
        new TypewriterMotionClip
        {
            action = UIMotionAction.Hide,
            duration = 0.12f,
            ease = Ease.Linear,
            deactivateOnComplete = false,
            restartFromBeginning = false,
            durationMode = TypewriterDurationMode.CharactersPerSecond,
            charactersPerSecond = 48f
        },
        new TypewriterMotionClip
        {
            action = UIMotionAction.Emphasis,
            duration = 0.36f,
            ease = Ease.Linear,
            restartFromBeginning = true,
            durationMode = TypewriterDurationMode.CharactersPerSecond,
            charactersPerSecond = 24f
        }
    };

    private TMP_Text targetText;
    private Tween currentTween;
    private int visibleCharacterCount;

    protected TMP_Text TargetText => targetText;
    protected bool UseUnscaledTime => useUnscaledTime;

    protected virtual void Awake()
    {
        EnsureReferences();
        SyncVisibleCharacterCountFromText();
    }

    protected virtual void OnDestroy()
    {
        Kill();
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

    public virtual void CompleteImmediate()
    {
        SetImmediate(UIMotionAction.Common);
    }

    public override bool SupportsAction(UIMotionAction action)
    {
        return action switch
        {
            UIMotionAction.Show => HasClip(UIMotionAction.Show),
            UIMotionAction.Common => HasClip(UIMotionAction.Common),
            UIMotionAction.Hide => HasClip(UIMotionAction.Hide),
            UIMotionAction.Emphasis => HasClip(UIMotionAction.Emphasis),
            _ => false
        };
    }

    public override Tween Play(UIMotionAction action, float delay = 0f)
    {
        return action switch
        {
            UIMotionAction.Show => SupportsAction(action) ? PlayTypewriter(action, delay) : null,
            UIMotionAction.Common => SupportsAction(action) ? PlayTypewriter(action, delay) : null,
            UIMotionAction.Hide => SupportsAction(action) ? PlayTypewriter(action, delay) : null,
            UIMotionAction.Emphasis => SupportsAction(action) ? PlayTypewriter(action, delay) : null,
            _ => null
        };
    }

    public override void SetImmediate(UIMotionAction action)
    {
        EnsureReferences();
        RefreshDefaults();
        Kill();

        int fullCharacterCount = GetFullCharacterCount();
        int targetVisibleCount = action == UIMotionAction.Hide ? 0 : fullCharacterCount;
        ApplyVisibleCharacterCount(targetVisibleCount);
    }

    public override void RefreshDefaults()
    {
        EnsureReferences();
        SyncVisibleCharacterCountFromText();
    }

    public override void Kill()
    {
        currentTween?.Kill();
        currentTween = null;
    }

    protected bool HasClip(UIMotionAction action)
    {
        for (int i = 0; i < actionClips.Count; i++)
        {
            TypewriterMotionClip clip = actionClips[i];
            if (clip != null && clip.action == action)
            {
                return true;
            }
        }

        return false;
    }

    protected virtual TypewriterMotionClip GetClip(UIMotionAction action)
    {
        for (int i = 0; i < actionClips.Count; i++)
        {
            TypewriterMotionClip clip = actionClips[i];
            if (clip != null && clip.action == action)
            {
                return clip;
            }
        }

        Debug.LogWarning($"{GetType().Name} missing motion clip for action '{action}'.", this);
        return new TypewriterMotionClip { action = action };
    }

    // 扩展说明：子类可覆盖起止字符数，扩展为分段 reveal、按词 reveal 或局部重播。
    protected virtual Tween PlayTypewriter(UIMotionAction action, float delay)
    {
        EnsureReferences();
        RefreshDefaults();
        Kill();

        TypewriterMotionClip clip = GetClip(action);
        int fullCharacterCount = GetFullCharacterCount();
        int fromCount = ResolveStartVisibleCharacterCount(action, clip, fullCharacterCount);
        int toCount = ResolveTargetVisibleCharacterCount(action, fullCharacterCount);
        float tweenDuration = ResolveTweenDuration(clip, fromCount, toCount);

        ApplyVisibleCharacterCount(fromCount);
        if (Mathf.Approximately(tweenDuration, 0f) || fromCount == toCount)
        {
            CompleteTypewriterAction(action, toCount);
            return null;
        }

        int tweenVisibleCount = fromCount;
        currentTween = DOTween.To(() => tweenVisibleCount, value =>
            {
                tweenVisibleCount = value;
                ApplyVisibleCharacterCount(value);
            }, toCount, tweenDuration)
            .SetEase(clip.ease)
            .SetDelay(delay)
            .SetUpdate(useUnscaledTime)
            .OnComplete(() => CompleteTypewriterAction(action, toCount));

        return currentTween;
    }

    protected virtual int ResolveStartVisibleCharacterCount(UIMotionAction action, TypewriterMotionClip clip, int fullCharacterCount)
    {
        if (action == UIMotionAction.Hide)
        {
            return visibleCharacterCount;
        }

        if (clip.restartFromBeginning)
        {
            return 0;
        }

        return Mathf.Clamp(visibleCharacterCount, 0, fullCharacterCount);
    }

    protected virtual int ResolveTargetVisibleCharacterCount(UIMotionAction action, int fullCharacterCount)
    {
        return action == UIMotionAction.Hide ? 0 : fullCharacterCount;
    }

    protected virtual float ResolveTweenDuration(TypewriterMotionClip clip, int fromCount, int toCount)
    {
        int characterDelta = Mathf.Abs(toCount - fromCount);
        if (characterDelta == 0)
        {
            return 0f;
        }

        if (clip.durationMode == TypewriterDurationMode.CharactersPerSecond)
        {
            return characterDelta / Mathf.Max(1f, clip.charactersPerSecond);
        }

        return clip.duration;
    }

    protected virtual bool ShouldDeactivateOnComplete(UIMotionAction action)
    {
        TypewriterMotionClip clip = GetClip(action);
        if (clip.deactivateOnComplete)
        {
            Debug.LogWarning($"{GetType().Name} '{name}' ignores deactivateOnComplete. Page activation is owned by UIPageBase.", this);
        }

        return false;
    }

    protected void ApplyVisibleCharacterCount(int characterCount)
    {
        EnsureReferences();
        int fullCharacterCount = GetFullCharacterCount();
        visibleCharacterCount = Mathf.Clamp(characterCount, 0, fullCharacterCount);
        targetText.maxVisibleCharacters = visibleCharacterCount;
    }

    protected int GetFullCharacterCount()
    {
        EnsureReferences();
        targetText.ForceMeshUpdate();
        return targetText.textInfo.characterCount;
    }

    private void CompleteTypewriterAction(UIMotionAction action, int targetVisibleCount)
    {
        ApplyVisibleCharacterCount(targetVisibleCount);
        ShouldDeactivateOnComplete(action);
    }

    private void EnsureReferences()
    {
        if (targetText == null)
        {
            targetText = GetComponent<TMP_Text>();
        }
    }

    private void SyncVisibleCharacterCountFromText()
    {
        int fullCharacterCount = GetFullCharacterCount();
        int currentVisibleCount = targetText.maxVisibleCharacters;
        if (currentVisibleCount >= fullCharacterCount)
        {
            visibleCharacterCount = fullCharacterCount;
            targetText.maxVisibleCharacters = fullCharacterCount;
            return;
        }

        visibleCharacterCount = Mathf.Clamp(currentVisibleCount, 0, fullCharacterCount);
        targetText.maxVisibleCharacters = visibleCharacterCount;
    }
}
