using System;

public interface IUIManager
{
    event EventHandler<UIPageEventArgs> PageOpened;
    event EventHandler<UIPageEventArgs> PageClosed;
    event EventHandler<UIPageEventArgs> PageActivationChanged;

    TPage OpenPage<TPage>(object payload = null) where TPage : UIPageBase;
    bool ClosePage<TPage>() where TPage : UIPageBase;
    bool CloseTopPage();
    int CloseAllPages();
    bool IsPageOpen<TPage>() where TPage : UIPageBase;
}
