using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Orange.UIFramework
{
    public readonly struct TooltipSessionHandle
    {
        private readonly Func<CloseReason, CancellationToken, UniTask> closeAsync;
        private readonly Func<CancellationToken, UniTask> pinAsync;
        private readonly Func<CancellationToken, UniTask> unpinAsync;
        private readonly Action<Vector2> updatePosition;

        internal TooltipSessionHandle(
            ViewHandle viewHandle,
            TooltipSessionMode sessionMode,
            TooltipContent content,
            TooltipChromeOptions chromeOptions,
            Func<CloseReason, CancellationToken, UniTask> closeAsync,
            Func<CancellationToken, UniTask> pinAsync,
            Func<CancellationToken, UniTask> unpinAsync,
            Action<Vector2> updatePosition)
        {
            ViewHandle = viewHandle;
            SessionMode = sessionMode;
            Content = content;
            ChromeOptions = chromeOptions;
            this.closeAsync = closeAsync;
            this.pinAsync = pinAsync;
            this.unpinAsync = unpinAsync;
            this.updatePosition = updatePosition;
        }

        public ViewHandle ViewHandle { get; }
        public string InstanceId => ViewHandle.InstanceId;
        public string ViewId => ViewHandle.ViewId;
        public TooltipSessionMode SessionMode { get; }
        public TooltipContent Content { get; }
        public TooltipChromeOptions ChromeOptions { get; }
        public UniTask ClosedTask => ViewHandle.ClosedTask;
        public bool IsValid => ViewHandle.IsValid && closeAsync != null;

        public UniTask CloseAsync(
            CloseReason reason = CloseReason.Normal,
            CancellationToken cancellationToken = default)
        {
            return closeAsync != null
                ? closeAsync.Invoke(reason, cancellationToken)
                : UniTask.CompletedTask;
        }

        public UniTask PinAsync(CancellationToken cancellationToken = default)
        {
            return pinAsync != null
                ? pinAsync.Invoke(cancellationToken)
                : UniTask.CompletedTask;
        }

        public UniTask UnpinAsync(CancellationToken cancellationToken = default)
        {
            return unpinAsync != null
                ? unpinAsync.Invoke(cancellationToken)
                : UniTask.CompletedTask;
        }

        public void UpdatePosition(Vector2 screenPosition)
        {
            updatePosition?.Invoke(screenPosition);
        }
    }
}
