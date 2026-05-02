using UnityEngine.Scripting.APIUpdating;

namespace AXR.Framework.UI
{
    [MovedFrom("")]
    public enum UIMotionPlaybackMode
{
    // 正常播放到结尾，Track 会创建 DOTween Tween。
    PlayToEnd,
    // 立即写入 Track 起点，用于准备隐藏态或回到初始态。
    SampleStart,
    // 立即写入 Track 终点，用于跳过动画或补齐缺失状态 Clip。
    SampleEnd
}
}
