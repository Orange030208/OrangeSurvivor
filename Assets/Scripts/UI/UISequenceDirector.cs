using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 页面内容导演：按组编排多个 UIRevealMotion 的入场和退场顺序。
/// 适合管理标题、按钮、卡片等页面内部元素的统一节奏。
/// </summary>
public class UISequenceDirector : MonoBehaviour
{
    [Serializable]
    private class MotionGroup
    {
        public string name;
        public List<UIRevealMotion> motions = new();
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
        foreach (MotionGroup group in enterGroups)
        {
            if (group == null || group.motions == null)
            {
                continue;
            }

            foreach (UIRevealMotion motion in group.motions)
            {
                motion?.PrepareEnter();
            }
        }
    }

    public void PlayEnter(Action onCompleted = null)
    {
        PrepareEnter();
        currentSequence = BuildSequence(enterGroups, true, false);
        currentSequence?.OnComplete(() =>
        {
            EnterCompleted?.Invoke();
            onCompleted?.Invoke();
        });
    }

    public void PlayExit(Action onCompleted = null)
    {
        Kill();
        List<MotionGroup> groups = GetExitGroupsOrFallback();
        currentSequence = BuildSequence(groups, false, reverseExitOrder);
        currentSequence?.OnComplete(() =>
        {
            ExitCompleted?.Invoke();
            onCompleted?.Invoke();
        });
    }

    public void CompleteImmediate()
    {
        Kill();
        foreach (MotionGroup group in enterGroups)
        {
            if (group == null || group.motions == null)
            {
                continue;
            }

            foreach (UIRevealMotion motion in group.motions)
            {
                motion?.CompleteImmediate();
            }
        }
    }

    public void SetHiddenImmediate()
    {
        Kill();
        foreach (MotionGroup group in enterGroups)
        {
            if (group == null || group.motions == null)
            {
                continue;
            }

            foreach (UIRevealMotion motion in group.motions)
            {
                motion?.SetHiddenImmediate();
            }
        }
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
        if (exitGroups != null && exitGroups.Count > 0)
        {
            return exitGroups;
        }

        return enterGroups;
    }

    private Sequence BuildSequence(List<MotionGroup> groups, bool isEnter, bool reverseGroups)
    {
        Sequence root = DOTween.Sequence().SetUpdate(useUnscaledTime);
        root.timeScale = timeScale;
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
                UIRevealMotion motion = group.motions[motionIndex];
                if (motion == null)
                {
                    continue;
                }

                float delay = group.playTogether ? order * group.stagger : 0f;
                Tween tween = isEnter ? motion.PlayEnter(delay) : motion.PlayExit(delay);
                if (tween != null)
                {
                    tween.timeScale = timeScale;
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
        foreach (MotionGroup group in groups)
        {
            if (group == null || group.motions == null)
            {
                continue;
            }

            foreach (UIRevealMotion motion in group.motions)
            {
                motion?.Kill();
            }
        }
    }
}
