namespace Orange.Services
{
    /// <summary>
    /// 需要在依赖可用后接收初始化回调的服务实现该接口。
    /// </summary>
    public interface IServiceInitializable
    {
        void Initialize(ServiceLifecycleContext context);
    }

    /// <summary>
    /// 需要在作用域初始化后接收启动回调的服务实现该接口。
    /// </summary>
    public interface IServiceStartable
    {
        void Start(ServiceLifecycleContext context);
    }

    /// <summary>
    /// 需要由所属作用域逐帧驱动的服务实现该接口。
    /// </summary>
    public interface IServiceTickable
    {
        void Tick(float deltaTime);
    }

    /// <summary>
    /// 需要在释放前接收有序关闭回调的服务实现该接口。
    /// </summary>
    public interface IServiceShutdown
    {
        void Shutdown(ServiceLifecycleContext context);
    }
}
