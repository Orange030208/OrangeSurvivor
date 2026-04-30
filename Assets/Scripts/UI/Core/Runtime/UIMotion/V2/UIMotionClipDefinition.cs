using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class UIMotionClipDefinition
{
    [SerializeField] private string clipId = UIMotionClipIds.SHOW;
    [SerializeField] private string channel = UIMotionChannelIds.VISIBILITY;
    [SerializeField] private UIMotionClipPlayMode playMode = UIMotionClipPlayMode.Parallel;
    [SerializeField] private UIMotionConflictPolicy conflictPolicy = UIMotionConflictPolicy.StopSameChannel;
    [SerializeField] [Min(0.01f)] private float durationScale = 1f;
    [SerializeReference] private List<UIMotionTrackDefinition> tracks = new();

    public string ClipId => string.IsNullOrWhiteSpace(clipId) ? UIMotionClipIds.SHOW : clipId;
    public string Channel => string.IsNullOrWhiteSpace(channel) ? UIMotionChannelIds.VISIBILITY : channel;
    public UIMotionClipPlayMode PlayMode => playMode;
    public UIMotionConflictPolicy ConflictPolicy => conflictPolicy;
    public float DurationScale => Mathf.Max(0.01f, durationScale);
    public IReadOnlyList<UIMotionTrackDefinition> Tracks => tracks;
}
