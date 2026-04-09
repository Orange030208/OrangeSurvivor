public interface IUIPage
{
    System.Type PageType { get; }
    string InstanceId { get; }
    bool IsVisible { get; }

    void HandleOpen(UIPageOpenContext context);
    void HandleClose();
    void HandleFocusChanged(bool hasFocus);
    void HandleTick(float deltaTime);
    void PlayOpenTransition(UIPageTransitionSettings transitionSettings, bool useUnscaledTime);
    void PlayCloseTransition(UIPageTransitionSettings transitionSettings, bool useUnscaledTime, System.Action onCompleted);
}
