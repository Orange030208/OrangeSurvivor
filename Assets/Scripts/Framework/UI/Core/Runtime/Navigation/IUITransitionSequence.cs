using UnityEngine.Scripting.APIUpdating;

namespace AXR.Framework.UI
{
    using System;

public interface IUITransitionSequence
{
    // 扩展说明：新增链式步骤时，继续返回 IUITransitionSequence 以保持统一编排体验。
    IUITransitionSequence ClosePage<TPage>() where TPage : UIPageBase;
    IUITransitionSequence CloseTopPage();
    IUITransitionSequence CloseAllPages();
    IUITransitionSequence OpenPage<TPage>(object payload = null) where TPage : UIPageBase;
    IUITransitionSequence Callback(Action action);
    IUITransitionSequence Callback<T>(Action<T> action, T argument);
    // 扩展说明：用于状态切换这类“先关闭旧页，再处理业务，最后开新页”的定制流程。
    void Play();
}
}
