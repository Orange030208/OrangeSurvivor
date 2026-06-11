using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Orange.UIFramework
{
    public sealed class PrefabViewLoader : IViewLoader
    {
        public UniTask<ViewBase> LoadAsync(
            ViewDefinition definition,
            Transform parent,
            CancellationToken cancellationToken)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            cancellationToken.ThrowIfCancellationRequested();

            GameObject prefab = definition.Prefab;
            if (prefab == null)
            {
                throw new MissingReferenceException($"LoadAsync failed: view definition '{definition.Id}' has no prefab.");
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab, parent, false);
            ViewBase view = instance.GetComponent<ViewBase>();
            if (view == null)
            {
                UnityEngine.Object.Destroy(instance);
                throw new InvalidOperationException($"LoadAsync failed: prefab '{prefab.name}' does not contain ViewBase on the root.");
            }

            return UniTask.FromResult(view);
        }

        public void Release(ViewBase view, ViewDefinition definition)
        {
            if (view == null)
            {
                return;
            }

            UnityEngine.Object.Destroy(view.gameObject);
        }
    }
}
