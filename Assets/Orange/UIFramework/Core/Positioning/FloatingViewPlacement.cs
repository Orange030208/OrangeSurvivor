using UnityEngine;

namespace Orange.UIFramework
{
    public readonly struct FloatingViewPlacement
    {
        public FloatingViewPlacement(
            Vector2 requestedPosition,
            Vector2 anchoredPosition,
            FloatingViewAnchor requestedAnchor,
            FloatingViewAnchor resolvedAnchor,
            bool wasFlipped,
            bool wasClamped,
            Rect localRect,
            Rect boundsRect)
        {
            HasValue = true;
            RequestedPosition = requestedPosition;
            AnchoredPosition = anchoredPosition;
            RequestedAnchor = requestedAnchor;
            ResolvedAnchor = resolvedAnchor;
            WasFlipped = wasFlipped;
            WasClamped = wasClamped;
            LocalRect = localRect;
            BoundsRect = boundsRect;
        }

        public bool HasValue { get; }
        public Vector2 RequestedPosition { get; }
        public Vector2 AnchoredPosition { get; }
        public FloatingViewAnchor RequestedAnchor { get; }
        public FloatingViewAnchor ResolvedAnchor { get; }
        public bool WasFlipped { get; }
        public bool WasClamped { get; }
        public Rect LocalRect { get; }
        public Rect BoundsRect { get; }
    }
}
