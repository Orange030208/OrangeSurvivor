using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Orange.UIFramework
{
    public interface IUIManager
    {
        UniTask<ViewHandle<TPage>> OpenPageAsync<TPage>(
            object payload = null,
            CancellationToken cancellationToken = default)
            where TPage : PageBase;

        UniTask<ViewHandle<TPage>> ReplacePageAsync<TPage>(
            object payload = null,
            CancellationToken cancellationToken = default)
            where TPage : PageBase;

        UniTask<ViewHandle<TPage>> ResetToPageAsync<TPage>(
            object payload = null,
            CancellationToken cancellationToken = default)
            where TPage : PageBase;

        UniTask CloseTopPageAsync(CancellationToken cancellationToken = default);
        UniTask CloseAllPagesAsync(CancellationToken cancellationToken = default);
        UniTask<bool> ClosePageAsync<TPage>(CancellationToken cancellationToken = default)
            where TPage : PageBase;

        UniTask<ViewHandle<TPopup>> ShowPopupAsync<TPopup>(
            object payload = null,
            PopupOptions options = default,
            CancellationToken cancellationToken = default)
            where TPopup : PopupBase;

        UniTask<ModalResult<TResult>> ShowModalAsync<TModal, TResult>(
            object payload = null,
            CancellationToken cancellationToken = default)
            where TModal : ModalBase<TResult>;

        UniTask<ViewHandle<TTooltip>> ShowTooltipAsync<TTooltip>(
            object payload,
            TooltipOptions options,
            CancellationToken cancellationToken = default)
            where TTooltip : TooltipBase;

        UniTask<ViewHandle<TToast>> ShowToastAsync<TToast>(
            object payload = null,
            ToastOptions options = default,
            CancellationToken cancellationToken = default)
            where TToast : ToastBase;

        UniTask ClearToastsAsync(CancellationToken cancellationToken = default);

        void UpdateTooltipPosition(Vector2 screenPosition);
        void HideTooltip();
        bool IsOpen<TView>() where TView : ViewBase;
    }
}
