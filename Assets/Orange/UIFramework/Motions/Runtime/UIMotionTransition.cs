using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Orange.UIFramework
{
    [DisallowMultipleComponent]
    public sealed class UIMotionTransition : MonoBehaviour, IViewTransition
    {
        private enum MotionSourceMode
        {
            Director,
            Player
        }

        [SerializeField] private MotionSourceMode sourceMode = MotionSourceMode.Director;
        [SerializeField] private UIMotionDirector director;
        [SerializeField] private UIMotionPlayer player;
        [SerializeField] private string enterSequenceId = UIMotionSequenceIds.ENTER;
        [SerializeField] private string exitSequenceId = UIMotionSequenceIds.EXIT;
        [SerializeField] private string enterClipId = UIMotionClipIds.SHOW;
        [SerializeField] private string exitClipId = UIMotionClipIds.HIDE;
        [SerializeField] private string hiddenClipId = UIMotionClipIds.HIDDEN;
        [SerializeField] private string visibleClipId = UIMotionClipIds.VISIBLE;
        [SerializeField] private bool hideImmediatelyBeforeEnter = true;
        [SerializeField] private bool showImmediatelyWhenSkipped = true;

        private void Awake()
        {
            ValidateSourceOrThrow();
        }

        public async UniTask PlayEnterAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (hideImmediatelyBeforeEnter)
            {
                RefreshDefaults();
                SetHiddenImmediate();
            }

            Tween tween = PlayEnterTween();
            if (tween == null)
            {
                if (showImmediatelyWhenSkipped)
                {
                    SetVisibleImmediate();
                }

                return;
            }

            await tween.WaitForCompletionAsync(cancellationToken);
        }

        public UniTask PlayExitAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Tween tween = PlayExitTween();
            return tween != null ? tween.WaitForCompletionAsync(cancellationToken) : UniTask.CompletedTask;
        }

        public void SetVisibleImmediate()
        {
            if (sourceMode == MotionSourceMode.Director)
            {
                ResolveDirectorOrThrow().SetImmediate(ResolveSequenceId(enterSequenceId, UIMotionSequenceIds.ENTER),
                    atEnd: true);
                return;
            }

            ResolvePlayerOrThrow().SetImmediate(ResolveClipId(visibleClipId, UIMotionClipIds.VISIBLE));
        }

        public void SetHiddenImmediate()
        {
            if (sourceMode == MotionSourceMode.Director)
            {
                ResolveDirectorOrThrow().SetImmediate(ResolveSequenceId(enterSequenceId, UIMotionSequenceIds.ENTER),
                    atEnd: false);
                return;
            }

            ResolvePlayerOrThrow().SetImmediate(ResolveClipId(hiddenClipId, UIMotionClipIds.HIDDEN));
        }

        public void Kill()
        {
            if (sourceMode == MotionSourceMode.Director)
            {
                ResolveDirectorOrThrow().Kill();
                return;
            }

            ResolvePlayerOrThrow().Kill();
        }

        private void RefreshDefaults()
        {
            if (sourceMode == MotionSourceMode.Director)
            {
                ResolveDirectorOrThrow().RefreshDefaults();
                return;
            }

            ResolvePlayerOrThrow().RefreshDefaults();
        }

        private Tween PlayEnterTween()
        {
            if (sourceMode == MotionSourceMode.Director)
            {
                return ResolveDirectorOrThrow().Play(ResolveSequenceId(enterSequenceId, UIMotionSequenceIds.ENTER));
            }

            return ResolvePlayerOrThrow().Play(ResolveClipId(enterClipId, UIMotionClipIds.SHOW));
        }

        private Tween PlayExitTween()
        {
            if (sourceMode == MotionSourceMode.Director)
            {
                return ResolveDirectorOrThrow().Play(ResolveSequenceId(exitSequenceId, UIMotionSequenceIds.EXIT));
            }

            return ResolvePlayerOrThrow().Play(ResolveClipId(exitClipId, UIMotionClipIds.HIDE));
        }

        private void ValidateSourceOrThrow()
        {
            if (sourceMode == MotionSourceMode.Director)
            {
                ResolveDirectorOrThrow();
                return;
            }

            ResolvePlayerOrThrow();
        }

        private UIMotionDirector ResolveDirectorOrThrow()
        {
            if (director == null)
            {
                throw new MissingComponentException(
                    $"{nameof(UIMotionTransition)} '{name}' requires a {nameof(UIMotionDirector)} reference.");
            }

            return director;
        }

        private UIMotionPlayer ResolvePlayerOrThrow()
        {
            if (player == null)
            {
                throw new MissingComponentException(
                    $"{nameof(UIMotionTransition)} '{name}' requires a {nameof(UIMotionPlayer)} reference.");
            }

            return player;
        }

        private static string ResolveSequenceId(string sequenceId, string fallbackSequenceId)
        {
            return string.IsNullOrWhiteSpace(sequenceId) ? fallbackSequenceId : sequenceId;
        }

        private static string ResolveClipId(string clipId, string fallbackClipId)
        {
            return string.IsNullOrWhiteSpace(clipId) ? fallbackClipId : clipId;
        }
    }
}
