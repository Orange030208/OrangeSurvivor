using Cysharp.Threading.Tasks;

namespace Orange.UIFramework
{
    internal interface IModalView
    {
        void CompleteResultIfNeeded(CloseReason reason);
    }

    public abstract class ModalBase<TResult> : ViewBase, IModalView
    {
        private UniTaskCompletionSource<ModalResult<TResult>> resultSource;
        private bool resultCompleted;

        public UniTask<ModalResult<TResult>> ResultTask
        {
            get
            {
                EnsureResultSource();
                return resultSource.Task;
            }
        }

        protected void SetResult(TResult value)
        {
            CompleteResult(ModalResult<TResult>.Confirm(value));
        }

        protected void Cancel(CloseReason reason = CloseReason.Cancel)
        {
            CompleteResult(ModalResult<TResult>.Cancel(reason));
        }

        void IModalView.CompleteResultIfNeeded(CloseReason reason)
        {
            if (resultCompleted)
            {
                return;
            }

            CompleteResult(ModalResult<TResult>.Cancel(reason));
        }

        protected override void OnInitialized(ViewHandle newHandle)
        {
            base.OnInitialized(newHandle);
            resultSource = null;
            resultCompleted = false;
        }

        private void CompleteResult(ModalResult<TResult> result)
        {
            EnsureResultSource();
            if (resultCompleted)
            {
                return;
            }

            resultCompleted = true;
            resultSource.TrySetResult(result);
        }

        private void EnsureResultSource()
        {
            if (resultSource != null)
            {
                return;
            }

            resultSource = new UniTaskCompletionSource<ModalResult<TResult>>();
            resultCompleted = false;
        }
    }
}
