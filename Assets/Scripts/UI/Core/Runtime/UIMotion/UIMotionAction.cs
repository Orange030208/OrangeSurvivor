/// <summary>
/// UI 动效语义动作。
/// - Show：进入展示语义对应的目标状态
/// - Hide：从当前状态进入隐藏态
/// - Press：按下态，常用于 pointer down
/// - Release：松开态，表示按下结束后的回弹/抬起反馈
/// - Emphasis：一次性的强调反馈，比如 click 后的脉冲
/// - Enter：鼠标进入时的反馈动画，常用于 hover enter
/// - Exit：鼠标移出时的反馈动画，常用于 hover exit
/// - Common：回到初始常态/默认态，用于从任意状态恢复到基准状态
/// </summary>
public enum UIMotionAction
{
    Show,
    Hide,
    Press,
    Release,
    Emphasis,
    Enter,
    Exit,
    Common
}
