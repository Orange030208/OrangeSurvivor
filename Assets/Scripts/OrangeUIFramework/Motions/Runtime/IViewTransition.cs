using System.Threading;
using Cysharp.Threading.Tasks;

namespace Orange.UIFramework
{
    public interface IViewTransition
    {
        UniTask PlayEnterAsync(CancellationToken cancellationToken);
        UniTask PlayExitAsync(CancellationToken cancellationToken);
        void SetVisibleImmediate();
        void SetHiddenImmediate();
        void Kill();
    }
}
