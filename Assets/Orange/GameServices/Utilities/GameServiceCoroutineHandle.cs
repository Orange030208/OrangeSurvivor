using UnityEngine;

namespace Orange.GameServices
{
    public readonly struct GameServiceCoroutineHandle
    {
        public GameServiceCoroutineHandle(Coroutine coroutine)
        {
            Coroutine = coroutine;
        }

        public Coroutine Coroutine { get; }
        public bool IsValid => Coroutine != null;
    }
}
