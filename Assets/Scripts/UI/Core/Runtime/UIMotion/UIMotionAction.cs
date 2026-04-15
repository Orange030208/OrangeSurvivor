/// <summary>
/// UI 动效语义动作。
/// - Show：进入正常展示态，也用于从高亮/按下恢复到普通态
/// - Hide：离开展示态
/// - Emphasis：一次性的强调反馈，比如 click 后的脉冲
/// - Highlight：高亮态，常用于 hover / selected / focused
/// - Press：按下态，常用于 pointer down
/// </summary>
public enum UIMotionAction
{
    Show,
    Hide,
    Emphasis,
    Highlight,
    Press
}
