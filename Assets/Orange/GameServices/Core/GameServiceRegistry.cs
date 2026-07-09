using System;
using System.Collections.Generic;

namespace Orange.GameServices
{
    /// <summary>
    /// 维护服务实例到具体类型以及显式合同类型的映射关系。
    /// </summary>
    public sealed class GameServiceRegistry
    {
        private readonly Dictionary<Type, GameService> servicesByContract = new Dictionary<Type, GameService>();
        private readonly List<GameService> services = new List<GameService>();
        private readonly GameServiceValidationReport report;

        internal GameServiceRegistry(GameServiceValidationReport report)
        {
            this.report = report;
        }

        public IReadOnlyList<GameService> Services => services;

        public void Register<TContract>(GameService service) where TContract : class
        {
            Register(typeof(TContract), service);
        }

        public void Register(Type contractType, GameService service)
        {
            if (contractType == null)
            {
                throw new ArgumentNullException(nameof(contractType));
            }

            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (!contractType.IsInstanceOfType(service))
            {
                report.AddError("Service does not implement the requested contract.", service.GetType(), contractType);
                return;
            }

            if (servicesByContract.TryGetValue(contractType, out GameService existingService) && existingService != service)
            {
                report.AddError(
                    $"Contract is already registered by {GameServiceTypeCache.GetDisplayName(existingService.GetType())}.",
                    service.GetType(),
                    contractType);
                return;
            }

            servicesByContract[contractType] = service;
        }

        internal void AddService(GameService service)
        {
            if (service == null)
            {
                return;
            }

            if (!services.Contains(service))
            {
                services.Add(service);
            }

            // 默认注册具体类型，这样服务可以在不额外声明合同的情况下被直接解析。
            Register(service.GetType(), service);
        }

        internal bool TryResolve(Type contractType, out GameService service)
        {
            return servicesByContract.TryGetValue(contractType, out service);
        }

        public bool TryGet<TContract>(out TContract service) where TContract : class
        {
            if (servicesByContract.TryGetValue(typeof(TContract), out GameService registeredService) &&
                registeredService is TContract typedService)
            {
                service = typedService;
                return true;
            }

            service = null;
            return false;
        }

        public TContract Get<TContract>() where TContract : class
        {
            if (TryGet(out TContract service))
            {
                return service;
            }

            throw new GameServiceException($"Game service contract '{GameServiceTypeCache.GetDisplayName(typeof(TContract))}' is not registered.");
        }
    }
}
