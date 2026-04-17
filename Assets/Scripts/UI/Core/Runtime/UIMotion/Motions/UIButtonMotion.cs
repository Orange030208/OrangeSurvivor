using UnityEngine;

/// <summary>
/// 按钮/列表项专用动效：只控制显影、缩放与旋转，不改布局位置。
/// 适合 click-only 交互目标、ScrollView 子项、受 LayoutGroup / ContentSizeFitter 驱动的 UI。
/// </summary>
public class UIButtonMotion : UIRevealMotion
{
    private void Reset()
    {
        ApplyConfigByString(BUTTON_OPTION);
    }
}
