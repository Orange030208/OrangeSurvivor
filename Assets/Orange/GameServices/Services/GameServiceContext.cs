using System;
using System.Collections;
using UnityEngine;

namespace Orange.GameServices
{
    /// <summary>
    /// 服务在 Attach 后可用的轻量运行时上下文。
    /// </summary>
    public sealed class GameServiceContext
    {
        private readonly GameServiceHost host;

        internal GameServiceContext(GameServiceHost host, GameServiceRoot root)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            Root = root != null ? root : throw new ArgumentNullException(nameof(root));
        }

        public string ScopeId => host.ScopeId;
        public GameServiceRoot Root { get; }
        public Transform RootTransform => Root.transform;
        public GameObject RootObject => Root.gameObject;

        public T Get<T>() where T : class
        {
            return host.Get<T>();
        }

        public bool TryGet<T>(out T service) where T : class
        {
            return host.TryGet(out service);
        }

        /// <summary>
        /// 协程实际运行在 Root MonoBehaviour 上，服务本身仍保持普通可序列化对象形态。
        /// </summary>
        public Coroutine StartCoroutine(IEnumerator routine)
        {
            if (routine == null)
            {
                throw new ArgumentNullException(nameof(routine));
            }

            return Root.StartCoroutine(routine);
        }

        public void StopCoroutine(Coroutine coroutine)
        {
            if (coroutine != null)
            {
                Root.StopCoroutine(coroutine);
            }
        }

        public void AddCleanup(Action cleanup)
        {
            host.AddCleanup(cleanup);
        }
    }
}
