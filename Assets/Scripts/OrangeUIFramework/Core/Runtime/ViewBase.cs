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
        private ViewHandle handle;
        private bool initialized;

        public string InstanceId => handle.InstanceId;
        public bool IsOpen { get; private set; }
        public bool InputActive { get; private set; }
        public bool BlocksRaycasts { get; private set; }
        public virtual bool RequiresTick => false;
        public ViewRuntimePhase Phase { get; private set; } = ViewRuntimePhase.None;
        protected ViewHandle Handle => handle;
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

                OnClosed(reason);
                IsOpen = false;
                canvasGroup.alpha = 0f;
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
