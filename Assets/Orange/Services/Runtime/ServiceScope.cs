using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Orange.Services
{
    /// <summary>
    /// Default service scope implementation with dependency-ordered lifecycle management.
    /// </summary>
    public sealed class ServiceScope : IServiceScope
    {
        private readonly ServiceScope parent;
        private readonly ServiceRegistry registry = new ServiceRegistry();
        private readonly Object owner;
        private readonly Dictionary<Type, ServiceNode> nodesByContract = new Dictionary<Type, ServiceNode>();
        private readonly List<ServiceNode> lifecycleNodes = new List<ServiceNode>();
        private readonly List<ServiceScope> childScopes = new List<ServiceScope>();

        public ServiceScope(string name = null, Object owner = null)
            : this(null, name, owner)
        {
        }

        private ServiceScope(ServiceScope parent, string name, Object owner)
        {
            this.parent = parent;
            this.owner = owner != null ? owner : parent?.owner;
            Name = string.IsNullOrWhiteSpace(name) ? "ServiceScope" : name;
        }

        public string Name { get; }
        public IServiceScope Parent => parent;
        public IServiceRegistry Registry => registry;
        public bool IsBuilt { get; private set; }
        public bool IsInitialized { get; private set; }
        public bool IsStarted { get; private set; }
        public bool IsShutdown { get; private set; }

        public void Install(IServiceInstaller installer)
        {
            if (installer == null)
            {
                throw new ArgumentNullException(nameof(installer));
            }

            installer.Install(registry);
        }

        public void Build()
        {
            ThrowIfDisposed();
            if (IsBuilt)
            {
                return;
            }

            registry.Freeze();
            foreach (KeyValuePair<Type, ServiceRegistrationDescriptor> pair in registry.Descriptors)
            {
                nodesByContract.Add(pair.Key, new ServiceNode(pair.Value));
            }

            ValidateDependencies();
            BuildLifecycleOrder();
            IsBuilt = true;
        }

        public void Initialize()
        {
            ThrowIfDisposed();
            EnsureBuilt();
            if (IsInitialized)
            {
                return;
            }

            IsInitialized = true;
            try
            {
                CreateEagerServices();
                for (int i = 0; i < lifecycleNodes.Count; i++)
                {
                    ServiceNode node = lifecycleNodes[i];
                    if (node.HasInstance)
                    {
                        InitializeNodeWithDependencies(node, new List<Type>());
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public void Start()
        {
            ThrowIfDisposed();
            EnsureBuilt();
            if (!IsInitialized)
            {
                Initialize();
            }

            if (IsStarted)
            {
                return;
            }

            IsStarted = true;
            for (int i = 0; i < lifecycleNodes.Count; i++)
            {
                ServiceNode node = lifecycleNodes[i];
                if (node.HasInstance)
                {
                    StartNodeWithDependencies(node, new List<Type>());
                }
            }
        }

        public void Tick(float deltaTime)
        {
            if (!IsStarted || IsShutdown)
            {
                return;
            }

            for (int i = 0; i < lifecycleNodes.Count; i++)
            {
                ServiceNode node = lifecycleNodes[i];
                if (!node.HasInstance || !node.IsStarted || node.IsShutdown)
                {
                    continue;
                }

                if (node.Instance is IServiceTickable tickable)
                {
                    try
                    {
                        tickable.Tick(deltaTime);
                    }
                    catch (Exception exception)
                    {
                        throw CreateLifecycleException(
                            node,
                            ServiceLifecyclePhase.Tick,
                            exception,
                            new List<Type> { node.Descriptor.ContractType });
                    }
                }
            }

            for (int i = 0; i < childScopes.Count; i++)
            {
                childScopes[i].Tick(deltaTime);
            }
        }

        public void Shutdown()
        {
            if (IsShutdown)
            {
                return;
            }

            for (int i = childScopes.Count - 1; i >= 0; i--)
            {
                childScopes[i].Shutdown();
            }

            for (int i = lifecycleNodes.Count - 1; i >= 0; i--)
            {
                ShutdownNode(lifecycleNodes[i]);
            }

            IsShutdown = true;
        }

        public void Dispose()
        {
            Shutdown();
            IsDisposed = true;
            if (parent != null)
            {
                parent.RemoveChild(this);
            }
        }

        public IServiceScope CreateChild(Action<IServiceRegistry> installServices = null, string name = null)
        {
            ThrowIfDisposed();
            EnsureBuilt();
            if (IsShutdown)
            {
                throw new ServiceException(
                    $"Service scope failure. Phase: {ServiceLifecyclePhase.None}. " +
                    $"Contract: <none>. Service: <none>. Scope: {Name}. Dependency chain: <none>. " +
                    "Cannot create a child scope from a shutdown scope.");
            }

            ServiceScope child = new ServiceScope(this, name, owner);
            installServices?.Invoke(child.Registry);
            child.Build();
            childScopes.Add(child);

            if (IsInitialized)
            {
                child.Initialize();
            }

            if (IsStarted)
            {
                child.Start();
            }

            return child;
        }

        public TService Resolve<TService>() where TService : class
        {
            return (TService)Resolve(typeof(TService));
        }

        public object Resolve(Type serviceType)
        {
            if (TryResolve(serviceType, out object service))
            {
                return service;
            }

            throw new ServiceException(
                $"Service resolution failure. Phase: {ServiceLifecyclePhase.None}. " +
                $"Contract: {FormatType(serviceType)}. Service: <none>. Scope: {Name}. " +
                "Dependency chain: <none>. The service contract is not registered.",
                serviceType,
                null,
                ServiceLifecyclePhase.None,
                Name);
        }

        public bool TryResolve<TService>(out TService service) where TService : class
        {
            if (TryResolve(typeof(TService), out object resolved))
            {
                service = (TService)resolved;
                return true;
            }

            service = null;
            return false;
        }

        public bool TryResolve(Type serviceType, out object service)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            ThrowIfDisposed();
            EnsureBuilt();
            if (IsShutdown)
            {
                throw new ServiceException(
                    $"Service resolution failure. Phase: {ServiceLifecyclePhase.None}. " +
                    $"Contract: {FormatType(serviceType)}. Service: <none>. Scope: {Name}. " +
                    "Dependency chain: <none>. Cannot resolve from a shutdown scope.",
                    serviceType,
                    null,
                    ServiceLifecyclePhase.None,
                    Name);
            }

            if (nodesByContract.TryGetValue(serviceType, out ServiceNode node))
            {
                service = ResolveLocalNode(node, new List<Type>());
                return true;
            }

            if (parent != null)
            {
                return parent.TryResolve(serviceType, out service);
            }

            service = null;
            return false;
        }

        private bool IsDisposed { get; set; }

        private void EnsureBuilt()
        {
            if (!IsBuilt)
            {
                Build();
            }
        }

        private void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(Name);
            }
        }

        private void CreateEagerServices()
        {
            for (int i = 0; i < lifecycleNodes.Count; i++)
            {
                ServiceNode node = lifecycleNodes[i];
                if (node.Descriptor.Eager)
                {
                    EnsureCreatedWithDependencies(node, new List<Type>());
                }
            }
        }

        private object ResolveLocalNode(ServiceNode node, List<Type> chain)
        {
            EnsureCreatedWithDependencies(node, chain);
            AlignNodeToCurrentPhase(node, chain);
            return node.Instance;
        }

        private void EnsureCreatedWithDependencies(ServiceNode node, List<Type> chain)
        {
            if (node.HasInstance)
            {
                return;
            }

            if (node.IsCreating)
            {
                chain.Add(node.Descriptor.ContractType);
                throw new ServiceException(
                    $"Service lifecycle failure in scope '{Name}'. " +
                    $"Phase: {ServiceLifecyclePhase.Create}. Contract: {FormatType(node.Descriptor.ContractType)}. " +
                    $"Service: {FormatType(node.Descriptor.ServiceType)}. " +
                    $"Dependency chain: {FormatChain(chain)}. Cyclic service creation detected.",
                    node.Descriptor.ContractType,
                    node.Descriptor.ServiceType,
                    ServiceLifecyclePhase.Create,
                    Name);
            }

            node.IsCreating = true;
            chain.Add(node.Descriptor.ContractType);

            try
            {
                ResolveDeclaredDependencies(node, chain);
                object instance = node.Descriptor.Create(this);
                if (instance == null)
                {
                    throw new ServiceException(
                        $"Service lifecycle failure in scope '{Name}'. " +
                        $"Phase: {ServiceLifecyclePhase.Create}. Contract: {FormatType(node.Descriptor.ContractType)}. " +
                        $"Service: {FormatType(node.Descriptor.ServiceType)}. " +
                        $"Dependency chain: {FormatChain(chain)}. Factory returned null.",
                        node.Descriptor.ContractType,
                        node.Descriptor.ServiceType,
                        ServiceLifecyclePhase.Create,
                        Name);
                }

                node.SetInstance(instance);
            }
            catch (ServiceException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw CreateLifecycleException(node, ServiceLifecyclePhase.Create, exception, chain);
            }
            finally
            {
                node.IsCreating = false;
                RemoveLast(chain, node.Descriptor.ContractType);
            }
        }

        private void ResolveDeclaredDependencies(ServiceNode node, List<Type> chain)
        {
            IReadOnlyList<Type> dependencies = node.Descriptor.Dependencies;
            for (int i = 0; i < dependencies.Count; i++)
            {
                Type dependencyType = dependencies[i];
                if (nodesByContract.TryGetValue(dependencyType, out ServiceNode dependencyNode))
                {
                    EnsureCreatedWithDependencies(dependencyNode, chain);
                    continue;
                }

                Resolve(dependencyType);
            }
        }

        private void AlignNodeToCurrentPhase(ServiceNode node, List<Type> chain)
        {
            if (IsInitialized)
            {
                InitializeNodeWithDependencies(node, chain);
            }

            if (IsStarted)
            {
                StartNodeWithDependencies(node, chain);
            }
        }

        private void InitializeNodeWithDependencies(ServiceNode node, List<Type> chain)
        {
            if (!node.HasInstance || node.IsInitialized)
            {
                return;
            }

            InitializeDeclaredDependencies(node, chain);
            chain.Add(node.Descriptor.ContractType);
            try
            {
                if (node.Instance is IServiceInitializable initializable)
                {
                    initializable.Initialize(CreateContext(ServiceLifecyclePhase.Initialize));
                }

                node.IsInitialized = true;
            }
            catch (Exception exception)
            {
                throw CreateLifecycleException(node, ServiceLifecyclePhase.Initialize, exception, chain);
            }
            finally
            {
                RemoveLast(chain, node.Descriptor.ContractType);
            }
        }

        private void StartNodeWithDependencies(ServiceNode node, List<Type> chain)
        {
            if (!node.HasInstance || node.IsStarted)
            {
                return;
            }

            if (!node.IsInitialized)
            {
                InitializeNodeWithDependencies(node, chain);
            }

            StartDeclaredDependencies(node, chain);
            chain.Add(node.Descriptor.ContractType);
            try
            {
                if (node.Instance is IServiceStartable startable)
                {
                    startable.Start(CreateContext(ServiceLifecyclePhase.Start));
                }

                node.IsStarted = true;
            }
            catch (Exception exception)
            {
                throw CreateLifecycleException(node, ServiceLifecyclePhase.Start, exception, chain);
            }
            finally
            {
                RemoveLast(chain, node.Descriptor.ContractType);
            }
        }

        private void InitializeDeclaredDependencies(ServiceNode node, List<Type> chain)
        {
            IReadOnlyList<Type> dependencies = node.Descriptor.Dependencies;
            for (int i = 0; i < dependencies.Count; i++)
            {
                Type dependencyType = dependencies[i];
                if (nodesByContract.TryGetValue(dependencyType, out ServiceNode dependencyNode))
                {
                    EnsureCreatedWithDependencies(dependencyNode, chain);
                    InitializeNodeWithDependencies(dependencyNode, chain);
                    continue;
                }

                Resolve(dependencyType);
            }
        }

        private void StartDeclaredDependencies(ServiceNode node, List<Type> chain)
        {
            IReadOnlyList<Type> dependencies = node.Descriptor.Dependencies;
            for (int i = 0; i < dependencies.Count; i++)
            {
                Type dependencyType = dependencies[i];
                if (nodesByContract.TryGetValue(dependencyType, out ServiceNode dependencyNode))
                {
                    EnsureCreatedWithDependencies(dependencyNode, chain);
                    StartNodeWithDependencies(dependencyNode, chain);
                    continue;
                }

                Resolve(dependencyType);
            }
        }

        private void ShutdownNode(ServiceNode node)
        {
            if (!node.HasInstance || node.IsShutdown)
            {
                return;
            }

            try
            {
                if (node.Instance is IServiceShutdown shutdown)
                {
                    shutdown.Shutdown(CreateContext(ServiceLifecyclePhase.Shutdown));
                }

                if (node.Instance is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                node.IsShutdown = true;
            }
            catch (Exception exception)
            {
                throw CreateLifecycleException(
                    node,
                    ServiceLifecyclePhase.Shutdown,
                    exception,
                    new List<Type> { node.Descriptor.ContractType });
            }
        }

        private ServiceLifecycleContext CreateContext(ServiceLifecyclePhase phase)
        {
            return new ServiceLifecycleContext(this, this, phase, owner);
        }

        private void ValidateDependencies()
        {
            foreach (KeyValuePair<Type, ServiceNode> pair in nodesByContract)
            {
                ServiceRegistrationDescriptor descriptor = pair.Value.Descriptor;
                IReadOnlyList<Type> dependencies = descriptor.Dependencies;
                for (int i = 0; i < dependencies.Count; i++)
                {
                    Type dependencyType = dependencies[i];
                    if (!HasRegistrationInHierarchy(dependencyType))
                    {
                        List<Type> chain = new List<Type>
                        {
                            descriptor.ContractType,
                            dependencyType
                        };
                        throw new ServiceException(
                            $"Service dependency failure in scope '{Name}'. " +
                            $"Phase: {ServiceLifecyclePhase.None}. Contract: {FormatType(descriptor.ContractType)}. " +
                            $"Service: {FormatType(descriptor.ServiceType)}. " +
                            $"Dependency chain: {FormatChain(chain)}. Missing dependency: {FormatType(dependencyType)}.",
                            descriptor.ContractType,
                            descriptor.ServiceType,
                            ServiceLifecyclePhase.None,
                            Name);
                    }
                }
            }
        }

        private bool HasRegistrationInHierarchy(Type contractType)
        {
            if (nodesByContract.ContainsKey(contractType))
            {
                return true;
            }

            return parent != null && parent.HasRegistrationInHierarchy(contractType);
        }

        private void BuildLifecycleOrder()
        {
            Dictionary<Type, int> visitStates = new Dictionary<Type, int>();
            foreach (KeyValuePair<Type, ServiceNode> pair in nodesByContract)
            {
                VisitNode(pair.Key, visitStates, new List<Type>());
            }
        }

        private void VisitNode(Type contractType, Dictionary<Type, int> visitStates, List<Type> chain)
        {
            if (visitStates.TryGetValue(contractType, out int state))
            {
                if (state == 1)
                {
                    chain.Add(contractType);
                    throw new ServiceException(
                        $"Service dependency failure in scope '{Name}'. " +
                        $"Phase: {ServiceLifecyclePhase.None}. Contract: {FormatType(contractType)}. " +
                        $"Service: {FormatType(nodesByContract[contractType].Descriptor.ServiceType)}. " +
                        $"Dependency chain: {FormatChain(chain)}. Cyclic service dependency detected.",
                        contractType,
                        nodesByContract[contractType].Descriptor.ServiceType,
                        ServiceLifecyclePhase.None,
                        Name);
                }

                return;
            }

            visitStates[contractType] = 1;
            chain.Add(contractType);
            ServiceNode node = nodesByContract[contractType];
            IReadOnlyList<Type> dependencies = node.Descriptor.Dependencies;
            for (int i = 0; i < dependencies.Count; i++)
            {
                Type dependencyType = dependencies[i];
                if (nodesByContract.ContainsKey(dependencyType))
                {
                    VisitNode(dependencyType, visitStates, chain);
                }
            }

            RemoveLast(chain, contractType);
            visitStates[contractType] = 2;
            lifecycleNodes.Add(node);
        }

        private void RemoveChild(ServiceScope child)
        {
            childScopes.Remove(child);
        }

        private ServiceException CreateLifecycleException(
            ServiceNode node,
            ServiceLifecyclePhase phase,
            Exception exception,
            List<Type> chain)
        {
            string message =
                $"Service lifecycle failure in scope '{Name}'. " +
                $"Phase: {phase}. Contract: {FormatType(node.Descriptor.ContractType)}. " +
                $"Service: {FormatType(node.Descriptor.ServiceType)}. " +
                $"Dependency chain: {FormatChain(chain)}.";

            return new ServiceException(
                message,
                node.Descriptor.ContractType,
                node.Descriptor.ServiceType,
                phase,
                Name,
                exception);
        }

        private static void RemoveLast(List<Type> chain, Type expectedType)
        {
            if (chain.Count == 0)
            {
                return;
            }

            int lastIndex = chain.Count - 1;
            if (chain[lastIndex] == expectedType)
            {
                chain.RemoveAt(lastIndex);
            }
        }

        private static string FormatType(Type type)
        {
            return type != null ? type.FullName : "<null>";
        }

        private static string FormatChain(IReadOnlyList<Type> chain)
        {
            if (chain == null || chain.Count == 0)
            {
                return "<none>";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < chain.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" -> ");
                }

                builder.Append(FormatType(chain[i]));
            }

            return builder.ToString();
        }
    }
}
