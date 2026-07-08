using System;
using UnityEngine;

namespace Orange.Services
{
    /// <summary>
    /// Unity 侧的组合根，用于创建并驱动根服务作用域。
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class ServiceHost : MonoBehaviour, IServiceResolver
    {
        [SerializeField] private bool dontDestroyOnLoad = false;

        public ServiceScope Scope { get; private set; }
        public IServiceResolver Resolver => Scope;

        protected virtual string ScopeName => GetType().Name;

        public TService Resolve<TService>() where TService : class
        {
            EnsureScope();
            return Scope.Resolve<TService>();
        }

        public object Resolve(Type serviceType)
        {
            EnsureScope();
            return Scope.Resolve(serviceType);
        }

        public bool TryResolve<TService>(out TService service) where TService : class
        {
            EnsureScope();
            return Scope.TryResolve(out service);
        }

        public bool TryResolve(Type serviceType, out object service)
        {
            EnsureScope();
            return Scope.TryResolve(serviceType, out service);
        }

        public IServiceScope CreateChildScope(Action<IServiceRegistry> installServices = null, string name = null)
        {
            EnsureScope();
            return Scope.CreateChild(installServices, name);
        }

        protected abstract void InstallServices(IServiceRegistry registry);

        protected virtual void Awake()
        {
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            Scope = new ServiceScope(ScopeName, this);
            InstallServices(Scope.Registry);
            Scope.Build();
            Scope.Initialize();
        }

        protected virtual void Start()
        {
            Scope?.Start();
        }

        protected virtual void Update()
        {
            Scope?.Tick(Time.deltaTime);
        }

        protected virtual void OnDestroy()
        {
            if (Scope == null)
            {
                return;
            }

            Scope.Dispose();
            Scope = null;
        }

        private void EnsureScope()
        {
            if (Scope == null)
            {
                throw new ServiceException($"{GetType().Name} has not built its service scope yet.");
            }
        }
    }
}
