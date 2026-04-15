using System;

internal interface IUITransitionRunnerHost
{
    bool ClosePage(Type pageType);
    bool CloseTopPage();
    int CloseAllPages();
    void OpenPage(Type pageType, object payload);
}
