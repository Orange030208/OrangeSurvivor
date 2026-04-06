namespace UniversalUI.Core.Runtime
{
    public interface IUIPage
    {
        System.Type PageType { get; }
        string InstanceId { get; }
        bool IsVisible { get; }

        /// <summary>
        /// 打开页面。
        /// </summary>
        void HandleOpen(UIPageOpenContext context);

        /// <summary>
        /// 关闭页面。
        /// </summary>
        void HandleClose();

        /// <summary>
        /// 更新页面焦点状态。
        /// </summary>
        void HandleFocusChanged(bool hasFocus);

        /// <summary>
        /// 驱动页面帧更新。
        /// </summary>
        void HandleTick(float deltaTime);
    }
}
