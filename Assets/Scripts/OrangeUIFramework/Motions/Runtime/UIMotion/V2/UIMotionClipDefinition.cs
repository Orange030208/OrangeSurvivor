
namespace Orange.UIFramework
{
    using System;
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
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
    [SerializeReference, TypeFilter("@Orange.UIFramework.UIMotionTrackTypeSelectionUtility.GetSelectableTrackTypes()")]
    [ListDrawerSettings(Expanded = true)]
    private List<UIMotionTrackDefinition> tracks = new();

    public string ClipId => string.IsNullOrWhiteSpace(clipId) ? UIMotionClipIds.SHOW : clipId;
    public string Channel => string.IsNullOrWhiteSpace(channel) ? UIMotionChannelIds.VISIBILITY : channel;
    public UIMotionClipPlayMode PlayMode => playMode;
    public UIMotionConflictPolicy ConflictPolicy => conflictPolicy;
    public float DurationScale => Mathf.Max(0.01f, durationScale);
    public IReadOnlyList<UIMotionTrackDefinition> Tracks => tracks;

    [ShowInInspector, ReadOnly, PropertyOrder(-10)]
    private string InspectorTitle => string.IsNullOrWhiteSpace(ClipId) ? "Clip" : ClipId;

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
            tracks = new List<UIMotionTrackDefinition>()
        };
    }

    [Button("Alpha"), ButtonGroup("Add Track"), PropertyOrder(20)]
    private void AddAlphaTrack() => AddTrack<UIAlphaMotionTrack>();

    [Button("Move"), ButtonGroup("Add Track"), PropertyOrder(21)]
    private void AddMoveTrack() => AddTrack<UIMoveMotionTrack>();

    [Button("Sidebar"), ButtonGroup("Add Track"), PropertyOrder(22)]
    private void AddSidebarTrack() => AddTrack<UISidebarMotionTrack>();

    [Button("Scale"), ButtonGroup("Add Track"), PropertyOrder(23)]
    private void AddScaleTrack() => AddTrack<UIScaleMotionTrack>();

    [Button("Rotate"), ButtonGroup("Add Track"), PropertyOrder(24)]
    private void AddRotateTrack() => AddTrack<UIRotateMotionTrack>();

    [Button("Graphic Color"), ButtonGroup("Add Track"), PropertyOrder(25)]
    private void AddGraphicColorTrack() => AddTrack<UIGraphicColorMotionTrack>();

    [Button("Image Fill"), ButtonGroup("Add Track"), PropertyOrder(26)]
    private void AddImageFillTrack() => AddTrack<UIImageFillMotionTrack>();

    [Button("Sprite Swap"), ButtonGroup("Add Track"), PropertyOrder(27)]
    private void AddSpriteSwapTrack() => AddTrack<UISpriteSwapMotionTrack>();

    [Button("Sprite Sequence"), ButtonGroup("Add Track"), PropertyOrder(28)]
    private void AddSpriteSequenceTrack() => AddTrack<UISpriteSequenceMotionTrack>();

    [Button("TMP Typewriter"), ButtonGroup("Add Track"), PropertyOrder(29)]
    private void AddTmpTypewriterTrack() => AddTrack<UITMPTypewriterMotionTrack>();

    [Button("Material Float"), ButtonGroup("Add Track"), PropertyOrder(30)]
    private void AddMaterialFloatTrack() => AddTrack<UIMaterialFloatMotionTrack>();

    [Button("Material Color"), ButtonGroup("Add Track"), PropertyOrder(31)]
    private void AddMaterialColorTrack() => AddTrack<UIMaterialColorMotionTrack>();

    [Button("Callback"), ButtonGroup("Add Track"), PropertyOrder(32)]
    private void AddCallbackTrack() => AddTrack<UICallbackMotionTrack>();

    private void AddTrack<T>()
        where T : UIMotionTrackDefinition, new()
    {
        tracks ??= new List<UIMotionTrackDefinition>();
        tracks.Add(new T());
    }
}
}
