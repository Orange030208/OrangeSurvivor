using System;
using System.Collections.Generic;
using UnityEngine;

namespace Orange.GameServices
{
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
