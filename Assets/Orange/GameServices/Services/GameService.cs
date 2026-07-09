using System;
using System.Collections;
using UnityEngine;

namespace Orange.GameServices
{
    /// <summary>
    /// 所有托管服务的基类。服务先由 Root/Profile 序列化持有，
    /// 再由 <see cref="GameServiceHost"/> 统一完成挂载、启动和驱动。
    /// </summary>
    [Serializable]
    public abstract class GameService
    {
        [SerializeField] private bool enabled = true;

        private GameServiceCleanupBag cleanupBag = new GameServiceCleanupBag();

        public bool Enabled => enabled;
        public GameServiceState State { get; internal set; } = GameServiceState.Created;

        /// <summary>
        /// 依赖排序前的第一层排序键。它适合表达稳定的默认顺序，
        /// 真正的强顺序关系仍应通过依赖声明来约束。
        /// </summary>
        public virtual int Order => 0;
        public virtual GameServiceTickMode TickMode => GameServiceTickMode.None;

        /// <summary>
        /// 控制生命周期回调抛异常时，Host 采用什么处理策略。
        /// </summary>
        public virtual GameServiceExecutionPolicy ExecutionPolicy => GameServiceExecutionPolicy.DisableService;

        /// <summary>
        /// 运行时上下文入口，可访问宿主 Root、同作用域服务以及协程能力。
        /// 在 Attach 之后可用。
        /// </summary>
        protected GameServiceContext Context { get; private set; }

        protected virtual void DeclareDependencies(GameServiceDependencyBuilder dependencies) { }
        protected virtual void RegisterContracts(GameServiceRegistry registry) { }
        protected virtual void OnAttach() { }
        protected virtual void OnStart() { }
        protected virtual void OnUpdate(float deltaTime) { }
        protected virtual void OnFixedUpdate(float fixedDeltaTime) { }
        protected virtual void OnLateUpdate(float deltaTime) { }
        protected virtual void OnApplicationPause(bool paused) { }
        protected virtual void OnApplicationFocus(bool focused) { }
        protected virtual void OnDispose() { }
        protected virtual void OnValidateService(GameServiceValidationReport report) { }

        protected void AddCleanup(Action cleanup)
        {
            cleanupBag.Add(cleanup);
        }

        /// <summary>
        /// 通过服务启动的协程会在服务释放时自动停止，避免场景清理遗漏。
        /// </summary>
        protected GameServiceCoroutineHandle StartServiceCoroutine(IEnumerator routine)
        {
            if (Context == null)
            {
                throw new GameServiceException("Cannot start a coroutine before the service is attached.");
            }

            Coroutine coroutine = Context.StartCoroutine(routine);
            return cleanupBag.AddCoroutine(Context, coroutine);
        }

        protected void StopServiceCoroutine(GameServiceCoroutineHandle handle)
        {
            cleanupBag.StopCoroutine(handle);
        }

        internal void AttachContext(GameServiceContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            cleanupBag = new GameServiceCleanupBag();
        }

        internal void ClearContext()
        {
            Context = null;
        }

        internal void InvokeDeclareDependencies(GameServiceDependencyBuilder dependencies)
        {
            DeclareDependencies(dependencies);
        }

        internal void InvokeRegisterContracts(GameServiceRegistry registry)
        {
            RegisterContracts(registry);
        }

        internal void InvokeOnAttach()
        {
            OnAttach();
        }

        internal void InvokeOnStart()
        {
            OnStart();
        }

        internal void InvokeOnUpdate(float deltaTime)
        {
            OnUpdate(deltaTime);
        }

        internal void InvokeOnFixedUpdate(float fixedDeltaTime)
        {
            OnFixedUpdate(fixedDeltaTime);
        }

        internal void InvokeOnLateUpdate(float deltaTime)
        {
            OnLateUpdate(deltaTime);
        }

        internal void InvokeOnApplicationPause(bool paused)
        {
            OnApplicationPause(paused);
        }

        internal void InvokeOnApplicationFocus(bool focused)
        {
            OnApplicationFocus(focused);
        }

        internal void InvokeOnDispose()
        {
            OnDispose();
        }

        internal void InvokeOnValidateService(GameServiceValidationReport report)
        {
            OnValidateService(report);
        }

        internal void DisposeCleanup()
        {
            cleanupBag.Dispose();
        }
    }
}
