#if UNITY_EDITOR
using System.Threading;
using AXR.Framework.UI;
using Cysharp.Threading.Tasks;

namespace Orange.UIFramework.Tests
{
    public sealed class RuntimeTestPageView : PageBase
    {
        public static RuntimeTestPageView LastInstance { get; private set; }

        public int OpenCount { get; private set; }
        public int ClosedCount { get; private set; }
        public CloseReason LastCloseReason { get; private set; }

        public static void ResetState()
        {
            LastInstance = null;
        }

        protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
        {
            LastInstance = this;
            OpenCount++;
            return UniTask.CompletedTask;
        }

        protected override void OnClosed(CloseReason reason)
        {
            ClosedCount++;
            LastCloseReason = reason;
        }
    }

    public sealed class SecondRuntimeTestPageView : PageBase
    {
        public static SecondRuntimeTestPageView LastInstance { get; private set; }

        public static void ResetState()
        {
            LastInstance = null;
        }

        protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
        {
            LastInstance = this;
            return UniTask.CompletedTask;
        }
    }

    public sealed class RuntimeSlowOpeningPageView : PageBase
    {
        public static RuntimeSlowOpeningPageView LastInstance { get; private set; }

        public static void ResetState()
        {
            LastInstance = null;
        }

        protected override async UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
        {
            LastInstance = this;
            await UniTask.DelayFrame(3, PlayerLoopTiming.Update, cancellationToken);
        }
    }

    public sealed class RuntimeSlowClosingPageView : PageBase
    {
        public static RuntimeSlowClosingPageView LastInstance { get; private set; }

        public int ClosingStartedCount { get; private set; }
        public int ClosedCount { get; private set; }
        public CloseReason LastCloseReason { get; private set; }

        public static void ResetState()
        {
            LastInstance = null;
        }

        protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
        {
            LastInstance = this;
            return UniTask.CompletedTask;
        }

        protected override async UniTask OnClosingAsync(CloseReason reason, CancellationToken cancellationToken)
        {
            ClosingStartedCount++;
            await UniTask.DelayFrame(3, PlayerLoopTiming.Update, cancellationToken);
        }

        protected override void OnClosed(CloseReason reason)
        {
            ClosedCount++;
            LastCloseReason = reason;
        }
    }

    public sealed class RuntimeTestPopupView : PopupBase
    {
        public static RuntimeTestPopupView LastInstance { get; private set; }

        public static void ResetState()
        {
            LastInstance = null;
        }

        protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
        {
            LastInstance = this;
            return UniTask.CompletedTask;
        }
    }

    public sealed class RuntimeTestTooltipView : TooltipBase
    {
        public static RuntimeTestTooltipView LastInstance { get; private set; }

        public static void ResetState()
        {
            LastInstance = null;
        }

        protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
        {
            LastInstance = this;
            return UniTask.CompletedTask;
        }
    }

    public sealed class RuntimeTestModalView : ModalBase<bool>
    {
        public static RuntimeTestModalView LastInstance { get; private set; }

        public int ClosedCount { get; private set; }
        public CloseReason LastCloseReason { get; private set; }

        public static void ResetState()
        {
            LastInstance = null;
        }

        public void ConfirmForTest(bool value)
        {
            SetResult(value);
        }

        public void CancelForTest(CloseReason reason = CloseReason.Cancel)
        {
            Cancel(reason);
        }

        protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
        {
            LastInstance = this;
            return UniTask.CompletedTask;
        }

        protected override void OnClosed(CloseReason reason)
        {
            ClosedCount++;
            LastCloseReason = reason;
        }
    }

    public static class RuntimeTestViewState
    {
        public static void Reset()
        {
            RuntimeTestPageView.ResetState();
            SecondRuntimeTestPageView.ResetState();
            RuntimeSlowOpeningPageView.ResetState();
            RuntimeSlowClosingPageView.ResetState();
            RuntimeTestPopupView.ResetState();
            RuntimeTestTooltipView.ResetState();
            RuntimeTestModalView.ResetState();
            LegacyRuntimeTestPageView.ResetState();
        }
    }

    public sealed class LegacyRuntimeTestPageView : UIPageBase
    {
        public static LegacyRuntimeTestPageView LastInstance { get; private set; }

        public int OpenedCount { get; private set; }
        public int ClosedCount { get; private set; }
        public UIPageOpenContext LastOpenContext { get; private set; }

        public static void ResetState()
        {
            LastInstance = null;
        }

        protected override void OnPageOpened(UIPageOpenContext context)
        {
            LastInstance = this;
            LastOpenContext = context;
            OpenedCount++;
        }

        protected override void OnPageClosed()
        {
            ClosedCount++;
        }
    }
}
#endif
