using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Orange.UIFramework
{
    internal static class DOTweenUniTaskExtensions
    {
        public static UniTask WaitForCompletionAsync(this Tween tween, CancellationToken cancellationToken)
        {
            if (tween == null || !tween.IsActive() || tween.IsComplete())
            {
                return UniTask.CompletedTask;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                tween.Kill();
                return UniTask.FromCanceled(cancellationToken);
            }

            UniTaskCompletionSource completionSource = new UniTaskCompletionSource();
            bool completed = false;
            CancellationTokenRegistration registration = default;

            void Complete()
            {
                if (completed)
                {
                    return;
                }

                completed = true;
                registration.Dispose();
                completionSource.TrySetResult();
            }

            void Cancel()
            {
                if (completed)
                {
                    return;
                }

                completed = true;
                tween.Kill();
                registration.Dispose();
                completionSource.TrySetCanceled(cancellationToken);
            }

            TweenCallback previousOnComplete = tween.onComplete;
            tween.onComplete = () =>
            {
                previousOnComplete?.Invoke();
                Complete();
            };

            TweenCallback previousOnKill = tween.onKill;
            tween.onKill = () =>
            {
                previousOnKill?.Invoke();
                Complete();
            };

            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(Cancel);
                if (completed)
                {
                    registration.Dispose();
                }
            }

            return completionSource.Task;
        }
    }
}
