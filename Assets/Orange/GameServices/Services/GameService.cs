using System;
using System.Collections;
using UnityEngine;

namespace Orange.GameServices
{
    [Serializable]
    public abstract class GameService
    {
        [SerializeField] private bool enabled = true;

        private GameServiceCleanupBag cleanupBag = new GameServiceCleanupBag();

        public bool Enabled => enabled;
        public GameServiceState State { get; internal set; } = GameServiceState.Created;

        public virtual int Order => 0;
        public virtual GameServiceTickMode TickMode => GameServiceTickMode.None;
        public virtual GameServiceExecutionPolicy ExecutionPolicy => GameServiceExecutionPolicy.DisableService;

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
