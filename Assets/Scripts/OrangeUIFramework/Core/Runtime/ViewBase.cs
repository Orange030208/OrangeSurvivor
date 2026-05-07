using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Orange.UIFramework
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class ViewBase : MonoBehaviour, IView
    {
        private CanvasGroup canvasGroup;
        private IViewTransition viewTransition;
        private ViewHandle handle;
        private bool initialized;
        private bool transitionResolved;

        public string InstanceId => handle.InstanceId;
        public bool IsOpen { get; private set; }
        public bool InputActive { get; private set; }
        public bool BlocksRaycasts { get; private set; }
        public virtual bool RequiresTick => false;
        public ViewRuntimePhase Phase { get; private set; } = ViewRuntimePhase.None;
        protected ViewHandle Handle => handle;
        protected UIManager OwnerUIManager => handle.Owner;
        protected CanvasGroup CanvasGroup => canvasGroup;

        protected virtual void Awake()
        {
            ResolveReferences();
        }

        public void Initialize(ViewHandle newHandle)
        {
            if (!newHandle.IsValid)
            {
                throw new System.ArgumentException("ViewBase.Initialize failed: handle is invalid.", nameof(newHandle));
            }

            ResolveReferences();
            handle = newHandle;
            initialized = true;
            Phase = ViewRuntimePhase.Loaded;
            OnInitialized(newHandle);
        }

        public void ApplyInputState(bool interactable, bool blocksRaycasts)
        {
            ResolveReferences();
            InputActive = interactable;
            BlocksRaycasts = blocksRaycasts;
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = blocksRaycasts;
            OnInputChanged(interactable, blocksRaycasts);
        }

        public void Tick(float deltaTime)
        {
            if (!IsOpen)
            {
                return;
            }

            OnTick(deltaTime);
        }

        internal async UniTask OpenInternalAsync(OpenContext context, CancellationToken cancellationToken)
        {
            if (!initialized)
            {
                throw new InvalidOperationException($"View '{name}' must be initialized before opening.");
            }

            ResolveReferences();
            cancellationToken.ThrowIfCancellationRequested();

            Phase = ViewRuntimePhase.Opening;
            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            ApplyInputState(false, false);

            try
            {
                await OnOpeningAsync(context, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                await PlayEnterTransitionAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                IsOpen = true;
                Phase = ViewRuntimePhase.Opened;
                await OnOpenedAsync(cancellationToken);
            }
            catch
            {
                IsOpen = false;
                Phase = ViewRuntimePhase.Failed;
                throw;
            }
        }

        internal async UniTask CloseInternalAsync(CloseReason reason, CancellationToken cancellationToken)
        {
            if (Phase == ViewRuntimePhase.Closing || Phase == ViewRuntimePhase.Closed || Phase == ViewRuntimePhase.Recycled)
            {
                return;
            }

            ResolveReferences();
            cancellationToken.ThrowIfCancellationRequested();

            Phase = ViewRuntimePhase.Closing;
            ApplyInputState(false, false);

            try
            {
                await OnClosingAsync(reason, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                await PlayExitTransitionAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                ResetTransitionForInactiveState();
                OnClosed(reason);
                IsOpen = false;
                canvasGroup.alpha = 1f;
                Phase = ViewRuntimePhase.Closed;
                gameObject.SetActive(false);
            }
            catch
            {
                Phase = ViewRuntimePhase.Failed;
                throw;
            }
        }

        internal void MarkRecycled()
        {
            IsOpen = false;
            Phase = ViewRuntimePhase.Recycled;
        }

        protected virtual void OnInitialized(ViewHandle newHandle)
        {
        }

        protected virtual UniTask OnOpeningAsync(OpenContext context, System.Threading.CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask OnOpenedAsync(System.Threading.CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask OnClosingAsync(CloseReason reason, System.Threading.CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        protected virtual void OnClosed(CloseReason reason)
        {
        }

        protected virtual void OnInputChanged(bool interactable, bool blocksRaycasts)
        {
        }

        protected virtual void OnTick(float deltaTime)
        {
        }

        private async UniTask PlayEnterTransitionAsync(CancellationToken cancellationToken)
        {
            IViewTransition transition = ResolveTransition();
            if (transition == null)
            {
                return;
            }

            await transition.PlayEnterAsync(cancellationToken);
        }

        private async UniTask PlayExitTransitionAsync(CancellationToken cancellationToken)
        {
            IViewTransition transition = ResolveTransition();
            if (transition == null)
            {
                return;
            }

            await transition.PlayExitAsync(cancellationToken);
        }

        private void ResetTransitionForInactiveState()
        {
            IViewTransition transition = ResolveTransition();
            transition?.SetVisibleImmediate();
        }

        private IViewTransition ResolveTransition()
        {
            if (transitionResolved)
            {
                return viewTransition;
            }

            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IViewTransition transition)
                {
                    viewTransition = transition;
                    break;
                }
            }

            transitionResolved = true;
            return viewTransition;
        }

        private void ResolveReferences()
        {
            if (canvasGroup != null)
            {
                return;
            }

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                throw new MissingComponentException($"View '{name}' requires a CanvasGroup.");
            }
        }
    }
}
