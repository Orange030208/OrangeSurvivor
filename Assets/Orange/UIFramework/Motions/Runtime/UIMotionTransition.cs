using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Orange.UIFramework
{
    [DisallowMultipleComponent]
    public sealed class UIMotionTransition : MonoBehaviour, IViewTransition
    {
        [SerializeField] private MonoBehaviour motionSource;
        [SerializeField] private bool autoResolveInChildren = true;
        [SerializeField] private bool hideImmediatelyBeforeEnter = true;
        [SerializeField] private bool showImmediatelyWhenSkipped = true;

        private IUISequenceMotion sequenceMotion;

        private void Awake()
        {
            ResolveMotionOrThrow();
        }

        public async UniTask PlayEnterAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IUISequenceMotion motion = ResolveMotionOrThrow();

            if (hideImmediatelyBeforeEnter)
            {
                motion.RefreshDefaults();
                motion.SetHiddenImmediate();
            }

            Tween tween = motion.PlayEnter();
            if (tween == null)
            {
                if (showImmediatelyWhenSkipped)
                {
                    motion.CompleteImmediate();
                }

                return;
            }

            await tween.WaitForCompletionAsync(cancellationToken);
        }

        public UniTask PlayExitAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Tween tween = ResolveMotionOrThrow().PlayExit();
            return tween.WaitForCompletionAsync(cancellationToken);
        }

        public void SetVisibleImmediate()
        {
            ResolveMotionOrThrow().CompleteImmediate();
        }

        public void SetHiddenImmediate()
        {
            ResolveMotionOrThrow().SetHiddenImmediate();
        }

        public void Kill()
        {
            ResolveMotionOrThrow().Kill();
        }

        private IUISequenceMotion ResolveMotionOrThrow()
        {
            if (sequenceMotion != null)
            {
                return sequenceMotion;
            }

            if (motionSource != null)
            {
                sequenceMotion = ResolveMotion(motionSource);
                if (sequenceMotion != null)
                {
                    return sequenceMotion;
                }
            }

            if (autoResolveInChildren)
            {
                MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < behaviours.Length; i++)
                {
                    MonoBehaviour behaviour = behaviours[i];
                    if (behaviour == null || ReferenceEquals(behaviour, this))
                    {
                        continue;
                    }

                    sequenceMotion = ResolveMotion(behaviour);
                    if (sequenceMotion != null)
                    {
                        motionSource = behaviour;
                        return sequenceMotion;
                    }
                }
            }

            throw new MissingComponentException(
                $"{nameof(UIMotionTransition)} '{name}' requires a component implementing {nameof(IUISequenceMotion)}.");
        }

        private IUISequenceMotion ResolveMotion(MonoBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return null;
            }

            if (behaviour is IUISequenceMotion directMotion)
            {
                return directMotion;
            }

            MonoBehaviour[] behaviours = behaviour.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IUISequenceMotion siblingMotion)
                {
                    return siblingMotion;
                }
            }

            return null;
        }
    }
}
