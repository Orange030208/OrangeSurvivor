public interface IUIPage
{
    System.Type PageType { get; }
    string InstanceId { get; }
    bool IsVisible { get; }

    void HandleOpen(UIPageOpenContext context);
    void HandleClose();
    void HandleFocusChanged(bool hasFocus);
    void HandleTick(float deltaTime);
}
