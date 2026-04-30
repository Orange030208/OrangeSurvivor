using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = ScriptableObjectMenuPaths.UI_MOTION_DEFINITION, fileName = "UIMotionDefinition")]
public sealed class UIMotionDefinition : ScriptableObject
{
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private List<UIMotionClipDefinition> clips = new();

    public bool UseUnscaledTime => useUnscaledTime;
    public IReadOnlyList<UIMotionClipDefinition> Clips => clips;

    public bool TryGetClip(string clipId, out UIMotionClipDefinition clip)
    {
        clip = null;
        if (string.IsNullOrWhiteSpace(clipId) || clips == null)
        {
            return false;
        }

        for (int i = 0; i < clips.Count; i++)
        {
            UIMotionClipDefinition candidate = clips[i];
            if (candidate == null)
            {
                continue;
            }

            if (!string.Equals(candidate.ClipId, clipId, StringComparison.Ordinal))
            {
                continue;
            }

            clip = candidate;
            return true;
        }

        return false;
    }

    public List<string> GetClipIds()
    {
        List<string> clipIds = new();
        if (clips == null)
        {
            return clipIds;
        }

        for (int i = 0; i < clips.Count; i++)
        {
            UIMotionClipDefinition clip = clips[i];
            if (clip == null || string.IsNullOrWhiteSpace(clip.ClipId))
            {
                continue;
            }

            if (!clipIds.Contains(clip.ClipId))
            {
                clipIds.Add(clip.ClipId);
            }
        }

        return clipIds;
    }
}
