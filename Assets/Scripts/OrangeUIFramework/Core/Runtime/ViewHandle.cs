using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Orange.UIFramework
{
    public readonly struct ViewHandle
    {
        private readonly Func<CloseReason, CancellationToken, UniTask> closeAsync;

        public ViewHandle(
            string instanceId,
            string viewId,
            ViewKind kind,
            UniTask closedTask,
            Func<CloseReason, CancellationToken, UniTask> closeAsync,
            ViewBase view = null)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                throw new ArgumentException("ViewHandle requires a non-empty instance id.", nameof(instanceId));
            }

            InstanceId = instanceId;
            ViewId = viewId ?? string.Empty;
            Kind = kind;
            ClosedTask = closedTask;
            this.closeAsync = closeAsync;
            View = view;
        }

        public string InstanceId { get; }
        public string ViewId { get; }
        public ViewKind Kind { get; }
        public UniTask ClosedTask { get; }
        public ViewBase View { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(InstanceId) && closeAsync != null;

        public UniTask CloseAsync(
            CloseReason reason = CloseReason.Normal,
            CancellationToken cancellationToken = default)
        {
            if (closeAsync == null)
            {
                return UniTask.CompletedTask;
            }

            return closeAsync.Invoke(reason, cancellationToken);
        }

        internal ViewHandle WithView(ViewBase view)
        {
            return new ViewHandle(InstanceId, ViewId, Kind, ClosedTask, closeAsync, view);
        }
    }

    public readonly struct ViewHandle<TView>
        where TView : ViewBase
    {
        private readonly ViewHandle handle;

        public ViewHandle(ViewHandle handle, TView view)
        {
            this.handle = handle;
            View = view;
        }

        public string InstanceId => handle.InstanceId;
        public string ViewId => handle.ViewId;
        public ViewKind Kind => handle.Kind;
        public TView View { get; }
        public UniTask ClosedTask => handle.ClosedTask;
        public bool IsValid => handle.IsValid && View != null;

        public ViewHandle AsUntyped()
        {
            return handle;
        }

        public UniTask CloseAsync(
            CloseReason reason = CloseReason.Normal,
            CancellationToken cancellationToken = default)
        {
            return handle.CloseAsync(reason, cancellationToken);
        }
    }
}
