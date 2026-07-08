using System;

namespace Orange.Services
{
    /// <summary>
    /// 持有已注册服务并驱动其生命周期的作用域。
    /// </summary>
    public interface IServiceScope : IServiceResolver, IDisposable
    {
        string Name { get; }
        IServiceScope Parent { get; }
        IServiceRegistry Registry { get; }
        bool IsBuilt { get; }
        bool IsInitialized { get; }
        bool IsStarted { get; }
        bool IsShutdown { get; }

        void Build();
        void Initialize();
        void Start();
        void Tick(float deltaTime);
        void Shutdown();
        IServiceScope CreateChild(Action<IServiceRegistry> installServices = null, string name = null);
    }
}
