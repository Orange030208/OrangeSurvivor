using System;
using DG.Tweening;

public sealed class SidebarRegionMotionGroup
{
    private readonly SidebarRegionMotion[] motions;

    public SidebarRegionMotionGroup(params SidebarRegionMotion[] motions)
    {
        this.motions = motions ?? Array.Empty<SidebarRegionMotion>();
    }

    public void SetVisible(bool visible)
    {
        for (int i = 0; i < motions.Length; i++)
        {
            motions[i]?.SetVisible(visible);
        }
    }

    public void RefreshDefaults()
    {
        for (int i = 0; i < motions.Length; i++)
        {
            motions[i]?.RefreshDefaults();
        }
    }

    public void SetHiddenImmediate()
    {
        for (int i = 0; i < motions.Length; i++)
        {
            motions[i]?.SetHiddenImmediate();
        }
    }

    public void Kill()
    {
        for (int i = 0; i < motions.Length; i++)
        {
            motions[i]?.Kill();
        }
    }

    public void PlayHideAll(Action onCompleted)
    {
        int pendingCount = 0;
        bool completionInvoked = false;

        void MarkCompleted()
        {
            if (completionInvoked)
            {
                return;
            }

            pendingCount--;
            if (pendingCount <= 0)
            {
                completionInvoked = true;
                onCompleted?.Invoke();
            }
        }

        for (int i = 0; i < motions.Length; i++)
        {
            Tween tween = motions[i]?.PlayHide();
            if (tween == null)
            {
                continue;
            }

            pendingCount++;
            AppendLifecycleCallback(tween, MarkCompleted);
        }

        if (pendingCount == 0)
        {
            completionInvoked = true;
            onCompleted?.Invoke();
        }
    }

    private static void AppendLifecycleCallback(Tween tween, Action callback)
    {
        if (tween == null || callback == null)
        {
            return;
        }

        bool invoked = false;

        void InvokeOnce()
        {
            if (invoked)
            {
                return;
            }

            invoked = true;
            callback();
        }

        TweenCallback previousOnComplete = tween.onComplete;
        tween.onComplete = () =>
        {
            previousOnComplete?.Invoke();
            InvokeOnce();
        };

        TweenCallback previousOnKill = tween.onKill;
        tween.onKill = () =>
        {
            previousOnKill?.Invoke();
            InvokeOnce();
        };
    }
}
