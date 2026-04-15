public interface IUIPage
{
    System.Type PageType { get; }
    string InstanceId { get; }
    bool IsVisible { get; }

    void HandleOpen(UIPageOpenContext context);
    void HandleClose();
    void HandleActivationChanged(bool visualActive, bool inputActive);
    void HandleTick(float deltaTime);

    /// <summary>
    /// 触发页面进入流程，不要求等待 enter 动画完成。
    /// </summary>
    void PlayOpenTransition(bool useUnscaledTime);

    /// <summary>
    /// 触发页面离开流程；只有在页面关闭等待链路全部完成后才调用 onCompleted。
    /// 若页面启用了 UISequenceDirector，则会等待其 exit 完成。
    /// </summary>
    void PlayCloseTransition(bool useUnscaledTime, System.Action onCompleted);
}
