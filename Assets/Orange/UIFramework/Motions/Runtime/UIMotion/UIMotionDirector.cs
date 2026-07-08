namespace Orange.UIFramework
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using DG.Tweening;
    using UnityEngine;

    [DisallowMultipleComponent]
    public sealed class UIMotionDirector : MonoBehaviour
    {
        [Serializable]
        private sealed class MotionStep
        {
            public UIMotionPlayer player;
            public string clipId = UIMotionClipIds.SHOW;
            [Min(0f)] public float delay;
        }

        [Serializable]
        private sealed class MotionGroup
        {
            public string name;
            public List<MotionStep> steps = new();
            [Min(0f)] public float startDelay;
            [Min(0f)] public float stagger = 0.04f;
            public bool playTogether = true;
        }

        [Serializable]
        private sealed class MotionSequence
        {
            public string sequenceId = UIMotionSequenceIds.ENTER;
            public List<MotionGroup> groups = new();
        }

        [SerializeField] private List<MotionSequence> sequences = new();
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] [Min(0.01f)] private float timeScale = 1f;

        private Sequence currentSequence;

        public event Action<string> SequenceCompleted;

        private void OnDestroy()
        {
            Kill();
        }

        public Tween Play(string sequenceId, float delay = 0f)
        {
            Kill();

            MotionSequence sequenceDefinition = ResolveSequenceOrThrow(sequenceId);
            currentSequence = BuildSequence(sequenceDefinition, delay);
            currentSequence.OnComplete(() => SequenceCompleted?.Invoke(sequenceDefinition.sequenceId));
            return currentSequence;
        }

        public UniTask PlayAsync(string sequenceId, CancellationToken cancellationToken, float delay = 0f)
        {
            Tween tween = Play(sequenceId, delay);
            return tween.WaitForCompletionAsync(cancellationToken);
        }

        public void SetImmediate(string sequenceId, bool atEnd = true)
        {
            Kill();
            ForEachStep(ResolveSequenceOrThrow(sequenceId), step =>
            {
                step.player.SetImmediate(ResolveClipId(step.clipId), atEnd);
            });
        }

        public void RefreshDefaults()
        {
            HashSet<UIMotionPlayer> visitedPlayers = new();
            for (int sequenceIndex = 0; sequenceIndex < sequences.Count; sequenceIndex++)
            {
                MotionSequence sequence = sequences[sequenceIndex];
                if (sequence == null)
                {
                    continue;
                }

                ForEachStep(sequence, step =>
                {
                    if (visitedPlayers.Add(step.player))
                    {
                        step.player.RefreshDefaults();
                    }
                });
            }
        }

        public void Kill()
        {
            currentSequence?.Kill();
            currentSequence = null;
        }

        private Sequence BuildSequence(MotionSequence sequenceDefinition, float initialDelay)
        {
            Sequence root = DOTween.Sequence().SetUpdate(useUnscaledTime);
            root.timeScale = Mathf.Max(0.01f, timeScale);
            if (initialDelay > 0f)
            {
                root.AppendInterval(initialDelay);
            }

            if (sequenceDefinition.groups == null || sequenceDefinition.groups.Count == 0)
            {
                return root;
            }

            for (int groupIndex = 0; groupIndex < sequenceDefinition.groups.Count; groupIndex++)
            {
                MotionGroup group = sequenceDefinition.groups[groupIndex];
                if (group == null || group.steps == null || group.steps.Count == 0)
                {
                    continue;
                }

                Sequence groupSequence = DOTween.Sequence().SetUpdate(useUnscaledTime);
                if (group.startDelay > 0f)
                {
                    groupSequence.AppendInterval(group.startDelay);
                }

                int order = 0;
                for (int stepIndex = 0; stepIndex < group.steps.Count; stepIndex++)
                {
                    MotionStep step = ResolveStepOrThrow(group.steps[stepIndex]);
                    string clipId = ResolveClipId(step.clipId);
                    if (step.player.IsClipInfiniteLoop(clipId))
                    {
                        Debug.LogWarning(
                            $"{nameof(UIMotionDirector)} '{name}' skipped infinite loop clip '{clipId}' in sequence '{sequenceDefinition.sequenceId}'.",
                            this);
                        order++;
                        continue;
                    }

                    float stepDelay = step.delay;
                    if (group.playTogether)
                    {
                        stepDelay += order * Mathf.Max(0f, group.stagger);
                    }

                    Tween tween = step.player.Play(clipId, stepDelay);
                    if (tween == null)
                    {
                        order++;
                        continue;
                    }

                    if (group.playTogether)
                    {
                        groupSequence.Join(tween);
                    }
                    else
                    {
                        groupSequence.Append(tween);
                    }

                    order++;
                }

                root.Append(groupSequence);
            }

            return root;
        }

        private void ForEachStep(MotionSequence sequence, Action<MotionStep> action)
        {
            if (sequence?.groups == null)
            {
                return;
            }

            for (int groupIndex = 0; groupIndex < sequence.groups.Count; groupIndex++)
            {
                MotionGroup group = sequence.groups[groupIndex];
                if (group?.steps == null)
                {
                    continue;
                }

                for (int stepIndex = 0; stepIndex < group.steps.Count; stepIndex++)
                {
                    action(ResolveStepOrThrow(group.steps[stepIndex]));
                }
            }
        }

        private MotionSequence ResolveSequenceOrThrow(string sequenceId)
        {
            string resolvedSequenceId = ResolveSequenceId(sequenceId);
            for (int i = 0; i < sequences.Count; i++)
            {
                MotionSequence sequence = sequences[i];
                if (sequence == null)
                {
                    continue;
                }

                if (string.Equals(ResolveSequenceId(sequence.sequenceId), resolvedSequenceId, StringComparison.Ordinal))
                {
                    return sequence;
                }
            }

            throw new MissingReferenceException(
                $"{nameof(UIMotionDirector)} '{name}' could not find sequence '{resolvedSequenceId}'.");
        }

        private MotionStep ResolveStepOrThrow(MotionStep step)
        {
            if (step == null || step.player == null)
            {
                throw new MissingReferenceException($"{nameof(UIMotionDirector)} '{name}' contains a missing motion step.");
            }

            return step;
        }

        private static string ResolveSequenceId(string sequenceId)
        {
            return string.IsNullOrWhiteSpace(sequenceId) ? UIMotionSequenceIds.ENTER : sequenceId;
        }

        private static string ResolveClipId(string clipId)
        {
            return string.IsNullOrWhiteSpace(clipId) ? UIMotionClipIds.SHOW : clipId;
        }
    }
}
