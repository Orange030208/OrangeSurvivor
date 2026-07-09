using System;
using System.Collections.Generic;
using UnityEngine;

namespace Orange.GameServices
{
    /// <summary>
    /// 集中管理服务级的清理回调与协程句柄，保证释放路径收敛。
    /// </summary>
    internal sealed class GameServiceCleanupBag : IDisposable
    {
        private readonly List<Action> cleanupActions = new List<Action>();
        private readonly List<GameServiceCoroutineHandle> coroutineHandles = new List<GameServiceCoroutineHandle>();
        private GameServiceContext context;
        private bool disposed;

        public void Add(Action cleanup)
        {
            if (cleanup == null)
            {
                return;
            }

            if (disposed)
            {
                cleanup.Invoke();
                return;
            }

            cleanupActions.Add(cleanup);
        }

        public GameServiceCoroutineHandle AddCoroutine(GameServiceContext ownerContext, Coroutine coroutine)
        {
            context = ownerContext;
            GameServiceCoroutineHandle handle = new GameServiceCoroutineHandle(coroutine);
            if (coroutine != null)
            {
                coroutineHandles.Add(handle);
            }

            return handle;
        }

        public void StopCoroutine(GameServiceCoroutineHandle handle)
        {
            if (!handle.IsValid || context == null)
            {
                return;
            }

            context.StopCoroutine(handle.Coroutine);
            for (int i = coroutineHandles.Count - 1; i >= 0; i--)
            {
                if (coroutineHandles[i].Coroutine == handle.Coroutine)
                {
                    coroutineHandles.RemoveAt(i);
                }
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            // 先停协程，再跑清理回调，避免清理逻辑和仍在执行的协程互相打架。
            StopCoroutines();
            RunCleanupActions();
            context = null;
        }

        private void StopCoroutines()
        {
            if (context == null)
            {
                coroutineHandles.Clear();
                return;
            }

            for (int i = coroutineHandles.Count - 1; i >= 0; i--)
            {
                Coroutine coroutine = coroutineHandles[i].Coroutine;
                if (coroutine != null)
                {
                    context.StopCoroutine(coroutine);
                }
            }

            coroutineHandles.Clear();
        }

        private void RunCleanupActions()
        {
            // 逆序执行更符合“先注册，后回收”的常见资源释放预期。
            for (int i = cleanupActions.Count - 1; i >= 0; i--)
            {
                try
                {
                    cleanupActions[i]?.Invoke();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            cleanupActions.Clear();
        }
    }
}
