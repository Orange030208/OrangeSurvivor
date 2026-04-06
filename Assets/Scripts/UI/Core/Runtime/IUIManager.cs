using System;

namespace UniversalUI.Core.Runtime
{
    public interface IUIManager
    {
        event EventHandler<UIPageEventArgs> PageOpened;
        event EventHandler<UIPageEventArgs> PageClosed;
        event EventHandler<UIPageEventArgs> PageFocusChanged;

        /// <summary>
        /// 打开页面。
        /// </summary>
        TPage OpenPage<TPage>(object payload = null) where TPage : UIPageBase;

        /// <summary>
        /// 关闭指定页面最新实例。
        /// </summary>
        bool ClosePage<TPage>() where TPage : UIPageBase;

        /// <summary>
        /// 关闭返回栈顶部页面。
        /// </summary>
        bool CloseTopPage();

        /// <summary>
        /// 关闭全部打开的页面。
        /// </summary>
        int CloseAllPages();

        /// <summary>
        /// 检查页面是否打开。
        /// </summary>
        bool IsPageOpen<TPage>() where TPage : UIPageBase;
    }
}
