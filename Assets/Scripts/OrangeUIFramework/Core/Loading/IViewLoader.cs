using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Orange.UIFramework
{
    public interface IViewLoader
    {
        UniTask<ViewBase> LoadAsync(
            ViewDefinition definition,
            Transform parent,
            CancellationToken cancellationToken);

        void Release(ViewBase view, ViewDefinition definition);
    }
}
