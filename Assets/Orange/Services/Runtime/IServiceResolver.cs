using System;

namespace Orange.Services
{
    /// <summary>
    /// 用于解析某个作用域可见的服务。
    /// </summary>
    public interface IServiceResolver
    {
        TService Resolve<TService>() where TService : class;
        object Resolve(Type serviceType);
        bool TryResolve<TService>(out TService service) where TService : class;
        bool TryResolve(Type serviceType, out object service);
    }
}
