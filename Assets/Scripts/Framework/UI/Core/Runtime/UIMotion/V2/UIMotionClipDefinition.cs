using UnityEngine.Scripting.APIUpdating;

namespace AXR.Framework.UI
{
    using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
// 一个 Clip 表示一次可播放的 UI 动作，例如 Show、Hide、HoverIn。
// Clip 只描述播放关系和冲突策略，实际属性变化由 Track 列表负责。
public sealed class UIMotionClipDefinition
{
    [SerializeField] private string clipId = UIMotionClipIds.SHOW;
    // Channel 用于冲突分组。页面显隐、按钮反馈、特殊强调动画可以放在不同 Channel 中互不干扰。
    [SerializeField] private string channel = UIMotionChannelIds.VISIBILITY;
    [SerializeField] private UIMotionClipPlayMode playMode = UIMotionClipPlayMode.Parallel;
    [SerializeField] private UIMotionConflictPolicy conflictPolicy = UIMotionConflictPolicy.StopSameChannel;
    // 用于整体拉伸或压缩 Clip 时长，不破坏各 Track 自身配置的相对比例。
    [SerializeField] [Min(0.01f)] private float durationScale = 1f;
    // SerializeReference 允许同一个列表保存不同类型的 Track，扩展新动画属性时无需修改 Clip 结构。
    [SerializeReference] private List<UIMotionTrackDefinition> tracks = new();

    public string ClipId => string.IsNullOrWhiteSpace(clipId) ? UIMotionClipIds.SHOW : clipId;
    public string Channel => string.IsNullOrWhiteSpace(channel) ? UIMotionChannelIds.VISIBILITY : channel;
    public UIMotionClipPlayMode PlayMode => playMode;
    public UIMotionConflictPolicy ConflictPolicy => conflictPolicy;
    public float DurationScale => Mathf.Max(0.01f, durationScale);
    public IReadOnlyList<UIMotionTrackDefinition> Tracks => tracks;
}
}
