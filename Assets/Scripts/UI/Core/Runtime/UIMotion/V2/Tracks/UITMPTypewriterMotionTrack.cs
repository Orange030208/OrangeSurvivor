using DG.Tweening;
using System;
using TMPro;
using UnityEngine;

[Serializable]
public sealed class UITMPTypewriterMotionTrack : UIMotionTrackDefinition
{
    private enum TypewriterStartMode
    {
        Current,
        Hidden,
        Full
    }

    private enum TypewriterEndMode
    {
        Hidden,
        Full
    }

    [SerializeField] private TypewriterStartMode startMode = TypewriterStartMode.Hidden;
    [SerializeField] private TypewriterEndMode endMode = TypewriterEndMode.Full;
    [SerializeField] private bool useCharactersPerSecond = true;
    [SerializeField] [Min(1f)] private float charactersPerSecond = 24f;

    protected override Tween CreateTrackTween(UIMotionTargetRegistry targets, UIMotionPlaybackContext context)
    {
        if (!targets.TryGetText(TargetKey, out TMP_Text text))
        {
            LogMissingTarget(nameof(TMP_Text));
            return null;
        }

        int fullCount = GetFullCharacterCount(text);
        int start = ResolveStartCount(text.maxVisibleCharacters, fullCount);
        int end = ResolveEndCount(fullCount);
        text.maxVisibleCharacters = start;

        float duration = ResolveTypewriterDuration(context, start, end);
        if (Mathf.Approximately(duration, 0f) || start == end)
        {
            text.maxVisibleCharacters = end;
            return null;
        }

        int current = start;
        return DOTween.To(() => current, value =>
            {
                current = value;
                text.maxVisibleCharacters = Mathf.Clamp(value, 0, fullCount);
            }, end, duration);
    }

    protected override void ApplySample(UIMotionTargetRegistry targets, float normalizedTime)
    {
        if (!targets.TryGetText(TargetKey, out TMP_Text text))
        {
            return;
        }

        int fullCount = GetFullCharacterCount(text);
        int start = ResolveStartCount(text.maxVisibleCharacters, fullCount);
        int end = ResolveEndCount(fullCount);
        text.maxVisibleCharacters = Mathf.RoundToInt(Mathf.LerpUnclamped(start, end, normalizedTime));
    }

    private float ResolveTypewriterDuration(UIMotionPlaybackContext context, int start, int end)
    {
        if (!useCharactersPerSecond)
        {
            return ResolveDuration(context);
        }

        int delta = Mathf.Abs(end - start);
        return delta / Mathf.Max(1f, charactersPerSecond);
    }

    private int ResolveStartCount(int currentCount, int fullCount)
    {
        return startMode switch
        {
            TypewriterStartMode.Hidden => 0,
            TypewriterStartMode.Full => fullCount,
            _ => Mathf.Clamp(currentCount, 0, fullCount)
        };
    }

    private int ResolveEndCount(int fullCount)
    {
        return endMode == TypewriterEndMode.Hidden ? 0 : fullCount;
    }

    private static int GetFullCharacterCount(TMP_Text text)
    {
        text.ForceMeshUpdate();
        return text.textInfo.characterCount;
    }
}
