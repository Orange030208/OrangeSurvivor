/// <summary>
/// UI 动效语义动作。
/// - Normal：正常展示态
/// - Hide：隐藏态
/// - Emphasis：一次性的强调反馈，比如 click 后的脉冲
/// - Highlight：高亮态，常用于 hover / selected / focused
/// - Press：按下态，常用于 pointer down
/// </summary>
public enum UIMotionAction
{
    Normal,
    Hide,
    Emphasis,
    Highlight,
    Press
}
