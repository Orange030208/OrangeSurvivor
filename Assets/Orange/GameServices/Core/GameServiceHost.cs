using System;
using System.Collections.Generic;
using UnityEngine;

namespace Orange.GameServices
{
    /// <summary>
    /// 持有一个服务作用域，负责校验、依赖排序、生命周期分发和销毁回收。
    /// </summary>
    public sealed class GameServiceHost
    {
        private readonly GameServiceRoot root;
        private readonly List<GameService> sourceServices = new List<GameService>();
        private readonly List<GameService> allServices = new List<GameService>();
        private readonly List<GameService> orderedServices = new List<GameService>();
        private readonly GameServiceCleanupBag cleanupBag = new GameServiceCleanupBag();

        private GameServiceRegistry registry;
        private GameServiceValidationReport validationReport = new GameServiceValidationReport();
        private GameServiceContext context;

        internal GameServiceHost(
            GameServiceRoot root,
            string scopeId,
            IEnumerable<GameService> services)
        {
            this.root = root != null ? root : throw new ArgumentNullException(nameof(root));
            ScopeId = string.IsNullOrWhiteSpace(scopeId) ? GameServiceRoot.DefaultScopeId : scopeId;

            if (services == null)
            {
                return;
            }

            foreach (GameService service in services)
            {
                sourceServices.Add(service);
            }
        }

        public string ScopeId { get; }
        public GameServiceState State { get; private set; } = GameServiceState.Created;
        public GameServiceValidationReport ValidationReport => validationReport;

        public T Get<T>() where T : class
        {
            EnsureRegistry();
            return registry.Get<T>();
        }

        public bool TryGet<T>(out T service) where T : class
        {
            if (registry == null)
            {
                service = null;
                return false;
            }

            return registry.TryGet(out service);
        }

        public void Attach()
        {
            if (State != GameServiceState.Created)
            {
                return;
            }

            // 在任何服务 Attach 之前先完成注册表构建，这样跨服务查询从一开始就可用。
            BuildRegistryAndOrder();
            ThrowIfValidationFailed("GameServices attach validation failed.");

            context = new GameServiceContext(this, root);
            for (int i = 0; i < orderedServices.Count; i++)
            {
                GameService service = orderedServices[i];
                service.AttachContext(context);
                if (InvokeService(service, service.InvokeOnAttach, "Attach"))
                {
                    service.State = GameServiceState.Attached;
                }
            }

            State = GameServiceState.Attached;
        }

        public void Start()
        {
            if (State != GameServiceState.Attached)
            {
                return;
            }

            for (int i = 0; i < orderedServices.Count; i++)
            {
                GameService service = orderedServices[i];
                if (service.State != GameServiceState.Attached)
                {
                    continue;
                }

                if (InvokeService(service, service.InvokeOnStart, "Start"))
                {
                    service.State = GameServiceState.Running;
                }
            }

            State = GameServiceState.Running;
        }

        public void Update(float deltaTime, float unscaledDeltaTime)
        {
            if (State != GameServiceState.Running)
            {
                return;
            }

            for (int i = 0; i < orderedServices.Count; i++)
            {
                GameService service = orderedServices[i];
                if (service.State != GameServiceState.Running)
                {
                    continue;
                }

                GameServiceTickMode tickMode = service.TickMode;
                if ((tickMode & GameServiceTickMode.UnscaledUpdate) != 0)
                {
                    InvokeService(service, () => service.InvokeOnUpdate(unscaledDeltaTime), "Update");
                }
                else if ((tickMode & GameServiceTickMode.Update) != 0)
                {
                    InvokeService(service, () => service.InvokeOnUpdate(deltaTime), "Update");
                }
            }
        }

        public void FixedUpdate(float fixedDeltaTime)
        {
            if (State != GameServiceState.Running)
            {
                return;
            }

            for (int i = 0; i < orderedServices.Count; i++)
            {
                GameService service = orderedServices[i];
                if (service.State == GameServiceState.Running &&
                    (service.TickMode & GameServiceTickMode.FixedUpdate) != 0)
                {
                    InvokeService(service, () => service.InvokeOnFixedUpdate(fixedDeltaTime), "FixedUpdate");
                }
            }
        }

        public void LateUpdate(float deltaTime)
        {
            if (State != GameServiceState.Running)
            {
                return;
            }

            for (int i = 0; i < orderedServices.Count; i++)
            {
                GameService service = orderedServices[i];
                if (service.State == GameServiceState.Running &&
                    (service.TickMode & GameServiceTickMode.LateUpdate) != 0)
                {
                    InvokeService(service, () => service.InvokeOnLateUpdate(deltaTime), "LateUpdate");
                }
            }
        }

        public void ApplicationPause(bool paused)
        {
            InvokeForRunningServices(service => service.InvokeOnApplicationPause(paused), "ApplicationPause");
        }

        public void ApplicationFocus(bool focused)
        {
            InvokeForRunningServices(service => service.InvokeOnApplicationFocus(focused), "ApplicationFocus");
        }

        public void Dispose()
        {
            if (State == GameServiceState.Disposed)
            {
                return;
            }

            for (int i = orderedServices.Count - 1; i >= 0; i--)
            {
                DisposeService(orderedServices[i]);
            }

            cleanupBag.Dispose();
            State = GameServiceState.Disposed;
            registry = null;
            context = null;
        }

        public void AddCleanup(Action cleanup)
        {
            cleanupBag.Add(cleanup);
        }

        public GameServiceSnapshot CaptureSnapshot()
        {
            List<GameServiceEntrySnapshot> serviceSnapshots = new List<GameServiceEntrySnapshot>();
            for (int i = 0; i < allServices.Count; i++)
            {
                GameService service = allServices[i];
                serviceSnapshots.Add(new GameServiceEntrySnapshot(
                    service.GetType(),
                    service.Enabled,
                    service.State,
                    service.Order,
                    service.TickMode,
                    service.ExecutionPolicy));
            }

            return new GameServiceSnapshot(
                ScopeId,
                State,
                serviceSnapshots,
                validationReport.Messages);
        }

        private void BuildRegistryAndOrder()
        {
            validationReport = new GameServiceValidationReport();
            registry = new GameServiceRegistry(validationReport);
            allServices.Clear();
            orderedServices.Clear();

            List<GameService> activeServices = CollectActiveServices();
            activeServices.Sort(CompareServiceOrder);

            for (int i = 0; i < activeServices.Count; i++)
            {
                // 具体服务类型始终会注册进去，即使它没有额外暴露接口合同。
                registry.AddService(activeServices[i]);
            }

            for (int i = 0; i < activeServices.Count; i++)
            {
                GameService service = activeServices[i];
                // 先做校验和合同注册，再做依赖排序，这样缺失合同会更早暴露，
                // 不会拖到 Attach/Start 阶段才失败。
                InvokeServiceValidation(service);
                InvokeRegisterContracts(service);
            }

            Dictionary<GameService, IReadOnlyList<GameServiceDependencyRule>> dependencyRules = CollectDependencyRules(activeServices);
            GameServiceDependencyGraph graph = new GameServiceDependencyGraph(
                activeServices,
                registry,
                dependencyRules,
                validationReport);
            orderedServices.AddRange(graph.Sort());
        }

        private List<GameService> CollectActiveServices()
        {
            List<GameService> activeServices = new List<GameService>();
            HashSet<GameService> seenServices = new HashSet<GameService>();

            for (int i = 0; i < sourceServices.Count; i++)
            {
                GameService service = sourceServices[i];
                if (service == null)
                {
                    validationReport.AddWarning("Null service entry will be ignored.");
                    continue;
                }

                if (!seenServices.Add(service))
                {
                    validationReport.AddError("The same service instance is registered more than once.", service.GetType());
                    continue;
                }

                allServices.Add(service);
                if (!service.Enabled)
                {
                    // 禁用服务仍会保留在诊断快照里，但不会进入运行时服务图。
                    service.State = GameServiceState.Disabled;
                    continue;
                }

                activeServices.Add(service);
            }

            return activeServices;
        }

        private Dictionary<GameService, IReadOnlyList<GameServiceDependencyRule>> CollectDependencyRules(List<GameService> activeServices)
        {
            Dictionary<GameService, IReadOnlyList<GameServiceDependencyRule>> rulesByService =
                new Dictionary<GameService, IReadOnlyList<GameServiceDependencyRule>>();

            for (int i = 0; i < activeServices.Count; i++)
            {
                GameService service = activeServices[i];
                GameServiceDependencyBuilder builder = new GameServiceDependencyBuilder();
                try
                {
                    service.InvokeDeclareDependencies(builder);
                }
                catch (Exception exception)
                {
                    validationReport.AddError($"DeclareDependencies failed: {exception.Message}", service.GetType());
                    Debug.LogException(exception, root);
                }

                rulesByService[service] = builder.Rules;
            }

            return rulesByService;
        }

        private void InvokeRegisterContracts(GameService service)
        {
            try
            {
                service.InvokeRegisterContracts(registry);
            }
            catch (Exception exception)
            {
                validationReport.AddError($"RegisterContracts failed: {exception.Message}", service.GetType());
                Debug.LogException(exception, root);
            }
        }

        private void InvokeServiceValidation(GameService service)
        {
            try
            {
                service.InvokeOnValidateService(validationReport);
            }
            catch (Exception exception)
            {
                validationReport.AddError($"OnValidateService failed: {exception.Message}", service.GetType());
                Debug.LogException(exception, root);
            }
        }

        private void InvokeForRunningServices(Action<GameService> action, string phase)
        {
            if (State != GameServiceState.Running)
            {
                return;
            }

            for (int i = 0; i < orderedServices.Count; i++)
            {
                GameService service = orderedServices[i];
                if (service.State == GameServiceState.Running)
                {
                    InvokeService(service, () => action(service), phase);
                }
            }
        }

        private bool InvokeService(GameService service, Action action, string phase)
        {
            try
            {
                action.Invoke();
                return true;
            }
            catch (Exception exception)
            {
                HandleServiceException(service, phase, exception);
                return false;
            }
        }

        private void HandleServiceException(GameService service, string phase, Exception exception)
        {
            string message = $"{phase} failed: {exception.Message}";
            validationReport.AddError(message, service.GetType());
            Debug.LogException(exception, root);

            // 处理策略决定异常是中断整体启动、保留服务继续运行，
            // 还是仅把当前故障服务移出图。
            switch (service.ExecutionPolicy)
            {
                case GameServiceExecutionPolicy.Throw:
                    service.State = GameServiceState.Faulted;
                    throw new GameServiceException(message, exception);
                case GameServiceExecutionPolicy.Continue:
                    return;
                default:
                    service.State = GameServiceState.Faulted;
                    DisposeService(service);
                    return;
            }
        }

        private void DisposeService(GameService service)
        {
            if (service == null || service.State == GameServiceState.Disposed || service.State == GameServiceState.Disabled)
            {
                return;
            }

            try
            {
                service.InvokeOnDispose();
            }
            catch (Exception exception)
            {
                validationReport.AddError($"Dispose failed: {exception.Message}", service.GetType());
                Debug.LogException(exception, root);
            }
            finally
            {
                // 即便 OnDispose 抛异常，清理袋里的资源也必须继续释放。
                service.DisposeCleanup();
                service.ClearContext();
                service.State = GameServiceState.Disposed;
            }
        }

        private void ThrowIfValidationFailed(string prefix)
        {
            if (validationReport.HasErrors)
            {
                throw new GameServiceException($"{prefix}\n{validationReport.FormatSummary()}");
            }
        }

        private void EnsureRegistry()
        {
            if (registry == null)
            {
                throw new GameServiceException("GameServiceHost has not been attached.");
            }
        }

        private static int CompareServiceOrder(GameService left, GameService right)
        {
            int orderComparison = left.Order.CompareTo(right.Order);
            if (orderComparison != 0)
            {
                return orderComparison;
            }

            return string.CompareOrdinal(
                GameServiceTypeCache.GetDisplayName(left.GetType()),
                GameServiceTypeCache.GetDisplayName(right.GetType()));
        }
    }
}
