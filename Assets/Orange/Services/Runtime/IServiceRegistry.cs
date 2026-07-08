using System;

namespace Orange.Services
{
    /// <summary>
    /// 在作用域 Build 之前可变的服务注册入口。
    /// </summary>
    public interface IServiceRegistry
    {
        bool IsFrozen { get; }

        ServiceRegistrationBuilder<TContract> Register<TContract>(
            Func<IServiceResolver, TContract> factory)
            where TContract : class;

        ServiceRegistrationBuilder<TContract> Register<TContract, TImplementation>()
            where TContract : class
            where TImplementation : class, TContract, new();

        ServiceRegistrationBuilder<TContract> RegisterInstance<TContract>(TContract instance)
            where TContract : class;

        ServiceRegistrationBuilder<TContract> Replace<TContract>(
            Func<IServiceResolver, TContract> factory)
            where TContract : class;

        ServiceRegistrationBuilder<TContract> Replace<TContract, TImplementation>()
            where TContract : class
            where TImplementation : class, TContract, new();

        ServiceRegistrationBuilder<TContract> ReplaceInstance<TContract>(TContract instance)
            where TContract : class;
    }
}
