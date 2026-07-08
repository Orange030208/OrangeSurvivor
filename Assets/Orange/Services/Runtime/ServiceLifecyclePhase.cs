namespace Orange.Services
{
    /// <summary>
    /// 标识服务作用域当前正在执行的生命周期阶段。
    /// </summary>
    public enum ServiceLifecyclePhase
    {
        None = 0,
        Create = 1,
        Initialize = 2,
        Start = 3,
        Tick = 4,
        Shutdown = 5,
        Dispose = 6
    }
}
