using UnityEngine.Scripting.APIUpdating;

namespace AXR.Framework.UI
{
    using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 页面内容导演：按组编排多个 UI 动作播放器的展示入场与隐藏退场顺序。
/// `PlayEnter()` 统一驱动 `Show` 语义，`PlayExit()` 统一驱动 `Hide` 语义。
/// </summary>
public class UISequenceDirector : MonoBehaviour, IUISequenceMotion
{
    [Serializable]
    private class MotionGroup
    {
        public string name;
        public List<MonoBehaviour> motions = new();
        public float startDelay;
        public float stagger = 0.04f;
        public bool playTogether = true;
    }
    
    [SerializeField] private List<MotionGroup> enterGroups = new();
    [SerializeField] private List<MotionGroup> exitGroups = new();
    [SerializeField] private bool reverseExitOrder = true;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] [Min(0.01f)] private float timeScale = 1f;

    private Sequence currentSequence;

    public event Action EnterCompleted;
    public event Action ExitCompleted;

    private void OnDestroy()
    {
        Kill();
    }

    public void PrepareEnter()
    {
        Kill();
        ForEachMotion(enterGroups, motion => motion.PrepareEnter());
    }

    public Tween PlayEnter(float delay = 0f)
    {
        PrepareEnter();
        currentSequence = BuildSequence(enterGroups, playEnter: true, reverseGroups: false, delay);
        currentSequence.OnComplete(() =>
        {
            EnterCompleted?.Invoke();
        });
        return currentSequence;
    }

    public Tween PlayExit(float delay = 0f)
    {
        Kill();
        List<MotionGroup> groups = GetExitGroupsOrFallback();
        currentSequence = BuildSequence(groups, playEnter: false, reverseGroups: reverseExitOrder, delay);
        currentSequence.OnComplete(() =>
        {
            ExitCompleted?.Invoke();
        });
        return currentSequence;
    }

    public void CompleteImmediate()
    {
        Kill();
        ForEachMotion(enterGroups, motion => motion.CompleteImmediate());
    }

    public void SetHiddenImmediate()
    {
        Kill();
        ForEachMotion(enterGroups, motion => motion.SetHiddenImmediate());
    }

    public void Kill()
    {
        currentSequence?.Kill();
        currentSequence = null;
        KillGroups(enterGroups);
        KillGroups(exitGroups);
    }

    private List<MotionGroup> GetExitGroupsOrFallback()
    {
        return exitGroups != null && exitGroups.Count > 0 ? exitGroups : enterGroups;
    }

    private Sequence BuildSequence(List<MotionGroup> groups, bool playEnter, bool reverseGroups, float initialDelay)
    {
        Sequence root = DOTween.Sequence().SetUpdate(useUnscaledTime);
        root.timeScale = timeScale;
        if (initialDelay > 0f)
        {
            root.AppendInterval(initialDelay);
        }

        if (groups == null || groups.Count == 0)
        {
            return root;
        }

        int start = reverseGroups ? groups.Count - 1 : 0;
        int end = reverseGroups ? -1 : groups.Count;
        int step = reverseGroups ? -1 : 1;

        for (int groupIndex = start; groupIndex != end; groupIndex += step)
        {
            MotionGroup group = groups[groupIndex];
            if (group == null || group.motions == null || group.motions.Count == 0)
            {
                continue;
            }

            Sequence groupSequence = DOTween.Sequence().SetUpdate(useUnscaledTime).SetDelay(group.startDelay);
            groupSequence.timeScale = timeScale;

            int motionStart = reverseGroups ? group.motions.Count - 1 : 0;
            int motionEnd = reverseGroups ? -1 : group.motions.Count;
            int motionStep = reverseGroups ? -1 : 1;
            int order = 0;

            for (int motionIndex = motionStart; motionIndex != motionEnd; motionIndex += motionStep)
            {
                IUISequenceMotion motion = ResolveMotion(group.motions[motionIndex]);

                float delay = group.playTogether ? order * group.stagger : 0f;
                Tween tween = playEnter ? motion.PlayEnter(delay) : motion.PlayExit(delay);
                if (tween != null)
                {
                    tween.timeScale = timeScale;
                }
                else
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

    private void KillGroups(List<MotionGroup> groups)
    {
        ForEachMotion(groups, motion => motion.Kill());
    }

    private void ForEachMotion(List<MotionGroup> groups, Action<IUISequenceMotion> action)
    {
        if (groups == null)
        {
            return;
        }

        foreach (MotionGroup group in groups)
        {
            if (group == null || group.motions == null)
            {
                continue;
            }

            foreach (MonoBehaviour behaviour in group.motions)
            {
                IUISequenceMotion motion = ResolveMotion(behaviour);
                action(motion);
            }
        }
    }

    private IUISequenceMotion ResolveMotion(MonoBehaviour behaviour)
    {
        if (behaviour == null)
        {
            throw new MissingReferenceException($"UISequenceDirector '{name}' contains a missing motion reference.");
        }

        if (ReferenceEquals(behaviour, this))
        {
            throw new InvalidOperationException($"UISequenceDirector '{name}' cannot reference itself as a motion.");
        }

        if (behaviour is IUISequenceMotion directMotion)
        {
            return directMotion;
        }

        MonoBehaviour[] siblingBehaviours = behaviour.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour siblingBehaviour in siblingBehaviours)
        {
            if (ReferenceEquals(siblingBehaviour, this))
            {
                continue;
            }

            if (siblingBehaviour is IUISequenceMotion siblingMotion)
            {
                return siblingMotion;
            }
        }

        throw new MissingComponentException(
            $"UISequenceDirector '{name}' expects reference '{behaviour.name}' to implement IUISequenceMotion, or share a GameObject with another IUISequenceMotion component that is not the director itself.");
    }
}
}
