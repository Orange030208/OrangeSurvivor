namespace Orange.UIFramework
{
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    // 运行时按真实 Transform 缓存初始状态。Track 的 target 为空时由 Player 自身作为默认目标。
    public sealed class UIMotionTargetCache
    {
        private readonly Dictionary<Transform, UIMotionTargetSnapshot> snapshotMap = new();
        private Transform owner;

        public void Initialize(Transform ownerTransform)
        {
            owner = ownerTransform;
            snapshotMap.Clear();
        }

        public void RefreshSnapshots(IEnumerable<UIMotionClipDefinition> clips)
        {
            snapshotMap.Clear();
            CaptureSnapshot(owner);

            if (clips == null)
            {
                return;
            }

            foreach (UIMotionClipDefinition clip in clips)
            {
                if (clip?.Tracks == null)
                {
                    continue;
                }

                IReadOnlyList<UIMotionTrackDefinition> tracks = clip.Tracks;
                for (int i = 0; i < tracks.Count; i++)
                {
                    CaptureSnapshot(ResolveTarget(tracks[i]));
                }
            }
        }

        public bool TryGetTarget(UIMotionTrackDefinition track, out Transform target)
        {
            target = ResolveTarget(track);
            return target != null;
        }

        public bool TryGetSnapshot(UIMotionTrackDefinition track, out UIMotionTargetSnapshot snapshot)
        {
            snapshot = null;
            Transform target = ResolveTarget(track);
            if (target == null)
            {
                return false;
            }

            if (snapshotMap.TryGetValue(target, out snapshot) && snapshot != null)
            {
                return true;
            }

            snapshot = new UIMotionTargetSnapshot(target);
            snapshotMap[target] = snapshot;
            return true;
        }

        public bool TryGetComponent<TComponent>(UIMotionTrackDefinition track, out TComponent component)
            where TComponent : Component
        {
            component = null;
            if (!TryGetTarget(track, out Transform target))
            {
                return false;
            }

            component = target.GetComponent<TComponent>();
            return component != null;
        }

        public bool TryGetRectTransform(UIMotionTrackDefinition track, out RectTransform rectTransform)
        {
            rectTransform = null;
            if (!TryGetTarget(track, out Transform target))
            {
                return false;
            }

            rectTransform = target as RectTransform;
            return rectTransform != null;
        }

        public bool TryGetCanvasGroup(UIMotionTrackDefinition track, out CanvasGroup canvasGroup)
        {
            return TryGetComponent(track, out canvasGroup);
        }

        public bool TryGetGraphic(UIMotionTrackDefinition track, out Graphic graphic)
        {
            return TryGetComponent(track, out graphic);
        }

        public bool TryGetImage(UIMotionTrackDefinition track, out Image image)
        {
            return TryGetComponent(track, out image);
        }

        public bool TryGetText(UIMotionTrackDefinition track, out TMP_Text text)
        {
            return TryGetComponent(track, out text);
        }

        public Transform ResolveTarget(UIMotionTrackDefinition track)
        {
            return track != null ? track.ResolveTarget(owner) : owner;
        }

        private void CaptureSnapshot(Transform target)
        {
            if (target == null || snapshotMap.ContainsKey(target))
            {
                return;
            }

            snapshotMap.Add(target, new UIMotionTargetSnapshot(target));
        }
    }
}
