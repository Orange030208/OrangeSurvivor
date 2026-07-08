using System;
using System.Collections.Generic;

namespace Orange.Services
{
    /// <summary>
    /// 在作用域完成 Build 前存放服务注册信息。
    /// </summary>
    public sealed class ServiceRegistry : IServiceRegistry
    {
        private readonly Dictionary<Type, ServiceRegistrationDescriptor> descriptorsByContract =
            new Dictionary<Type, ServiceRegistrationDescriptor>();

        public bool IsFrozen { get; private set; }

        internal IReadOnlyDictionary<Type, ServiceRegistrationDescriptor> Descriptors => descriptorsByContract;

        public ServiceRegistrationBuilder<TContract> Register<TContract>(
            Func<IServiceResolver, TContract> factory)
            where TContract : class
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return RegisterInternal<TContract>(
                typeof(TContract),
                typeof(TContract),
                resolver => factory.Invoke(resolver),
                false);
        }

        public ServiceRegistrationBuilder<TContract> Register<TContract, TImplementation>()
            where TContract : class
            where TImplementation : class, TContract, new()
        {
            return RegisterInternal<TContract>(
                typeof(TContract),
                typeof(TImplementation),
                _ => new TImplementation(),
                false);
        }

        public ServiceRegistrationBuilder<TContract> RegisterInstance<TContract>(TContract instance)
            where TContract : class
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return RegisterInternal<TContract>(
                typeof(TContract),
                instance.GetType(),
                _ => instance,
                false);
        }

        public ServiceRegistrationBuilder<TContract> Replace<TContract>(
            Func<IServiceResolver, TContract> factory)
            where TContract : class
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return RegisterInternal<TContract>(
                typeof(TContract),
                typeof(TContract),
                resolver => factory.Invoke(resolver),
                true);
        }

        public ServiceRegistrationBuilder<TContract> Replace<TContract, TImplementation>()
            where TContract : class
            where TImplementation : class, TContract, new()
        {
            return RegisterInternal<TContract>(
                typeof(TContract),
                typeof(TImplementation),
                _ => new TImplementation(),
                true);
        }

        public ServiceRegistrationBuilder<TContract> ReplaceInstance<TContract>(TContract instance)
            where TContract : class
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return RegisterInternal<TContract>(
                typeof(TContract),
                instance.GetType(),
                _ => instance,
                true);
        }

        internal void Freeze()
        {
            IsFrozen = true;
        }

        internal void EnsureMutable()
        {
            if (IsFrozen)
            {
                throw new ServiceException("Service registry is frozen after its scope has been built.");
            }
        }

        internal bool TryGetDescriptor(Type contractType, out ServiceRegistrationDescriptor descriptor)
        {
            return descriptorsByContract.TryGetValue(contractType, out descriptor);
        }

        private ServiceRegistrationBuilder<TContract> RegisterInternal<TContract>(
            Type contractType,
            Type serviceType,
            Func<IServiceResolver, object> factory,
            bool replace)
            where TContract : class
        {
            EnsureMutable();

            if (descriptorsByContract.ContainsKey(contractType) && !replace)
            {
                throw new ServiceException(
                    $"Service registration failure. Phase: {ServiceLifecyclePhase.None}. " +
                    $"Contract: {FormatType(contractType)}. Service: {FormatType(serviceType)}. " +
                    "Dependency chain: <none>. " +
                    "The contract is already registered. Use Replace to override it before Build.",
                    contractType,
                    serviceType,
                    ServiceLifecyclePhase.None,
                    null);
            }

            ServiceRegistrationDescriptor descriptor = new ServiceRegistrationDescriptor(
                contractType,
                serviceType,
                factory);
            descriptorsByContract[contractType] = descriptor;
            return new ServiceRegistrationBuilder<TContract>(this, descriptor);
        }

        private static string FormatType(Type type)
        {
            return type != null ? type.FullName : "<null>";
        }
    }
}
