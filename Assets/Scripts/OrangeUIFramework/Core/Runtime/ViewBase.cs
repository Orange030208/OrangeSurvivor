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
            OnInitialized(newHandle);
        }

        public void ApplyInputState(bool interactable, bool blocksRaycasts)
        {
            ResolveReferences();
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

        internal void MarkOpenState(bool isOpen)
        {
            IsOpen = isOpen;
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
