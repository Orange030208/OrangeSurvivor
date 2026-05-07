using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Orange.UIFramework
{
    public abstract class ViewPartBase : MonoBehaviour
    {
        public virtual void Bind(object context)
        {
        }

        public virtual void Unbind()
        {
        }

        public virtual UniTask ShowAsync(CancellationToken cancellationToken = default)
        {
            gameObject.SetActive(true);
            return UniTask.CompletedTask;
        }

        public virtual UniTask HideAsync(CancellationToken cancellationToken = default)
        {
            gameObject.SetActive(false);
            return UniTask.CompletedTask;
        }
    }
}
