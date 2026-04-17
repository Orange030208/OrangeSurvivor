using UnityEngine;

/// <summary>
/// 通用默认动效预设入口：适合标题、按钮、卡片等常规 UI 元素。
/// </summary>
public class UIDefaultMotion : UIRevealMotion
{
    private void Reset()
    {
        ApplyConfigByString(DEFAULT_OPTION);
    }
}
