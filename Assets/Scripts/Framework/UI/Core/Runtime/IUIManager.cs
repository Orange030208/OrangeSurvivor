using UnityEngine.Scripting.APIUpdating;

namespace AXR.Framework.UI
{
    using System;

public interface IUIManager
{
    event EventHandler<UIPageEventArgs> PageOpened;
    event EventHandler<UIPageEventArgs> PageClosed;
    event EventHandler<UIPageEventArgs> PageActivationChanged;

    // 立即打开目标页面；不会等待其他页面关闭完成。
    TPage OpenPage<TPage>(object payload = null) where TPage : UIPageBase;

    // 先关闭当前顶层页面，待其退场完成后再打开目标页面。
    void ReplaceTopPage<TPage>(object payload = null) where TPage : UIPageBase;

    // 关闭当前所有页面，待全部退场完成后只打开最后一次请求的目标页面。
    void ResetToPage<TPage>(object payload = null) where TPage : UIPageBase;

    // 扩展说明：通过链式序列自定义 UI 关闭、打开与业务回调的执行顺序。
    IUITransitionSequence BeginTransition();
    bool ClosePage<TPage>() where TPage : UIPageBase;
    bool CloseTopPage();
    int CloseAllPages();
    bool IsPageOpen<TPage>() where TPage : UIPageBase;
}
}
