
namespace Orange.UIFramework
{
    using System;
    using System.Collections.Generic;
    using DG.Tweening;
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
    // 1 表示播放一次，-1 表示无限循环；循环属于 Clip 播放策略，不属于单个 Track。
    [SerializeField] [Min(-1)] private int loopCount = 1;
    [SerializeField] private LoopType loopType = LoopType.Restart;
    [SerializeField] private bool autoPlayOnEnable;
    [SerializeField] private bool stopOnDisable = true;
    [SerializeField] private bool restoreOnDisable;
    // SerializeReference 允许同一个列表保存不同类型的 Track，扩展新动画属性时无需修改 Clip 结构。
    [SerializeReference]
    private List<UIMotionTrackDefinition> tracks = new();

    public string ClipId => string.IsNullOrWhiteSpace(clipId) ? UIMotionClipIds.SHOW : clipId;
    public string Channel => string.IsNullOrWhiteSpace(channel) ? UIMotionChannelIds.VISIBILITY : channel;
    public UIMotionClipPlayMode PlayMode => playMode;
    public UIMotionConflictPolicy ConflictPolicy => conflictPolicy;
    public float DurationScale => Mathf.Max(0.01f, durationScale);
    public int LoopCount => loopCount == 0 ? 1 : loopCount < -1 ? -1 : loopCount;
    public LoopType LoopType => loopType;
    public bool AutoPlayOnEnable => autoPlayOnEnable;
    public bool StopOnDisable => stopOnDisable;
    public bool RestoreOnDisable => restoreOnDisable;
    public bool IsInfiniteLoop => LoopCount < 0;
    public IReadOnlyList<UIMotionTrackDefinition> Tracks => tracks;

    public static UIMotionClipDefinition CreateDefault(
        string clipId,
        string channel,
        UIMotionConflictPolicy conflictPolicy)
    {
        return new UIMotionClipDefinition
        {
            clipId = clipId,
            channel = channel,
            playMode = UIMotionClipPlayMode.Parallel,
            conflictPolicy = conflictPolicy,
            durationScale = 1f,
            loopCount = 1,
            loopType = LoopType.Restart,
            autoPlayOnEnable = false,
            stopOnDisable = true,
            restoreOnDisable = false,
            tracks = new List<UIMotionTrackDefinition>()
        };
    }

}
}
