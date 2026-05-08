using UnityEngine;

namespace Orange.UIFramework
{
    public readonly struct TooltipOptions
    {
        private readonly bool valuesAssigned;
        private readonly bool useScreenPosition;
        private readonly float margin;

        public TooltipOptions(
            RectTransform anchor = null,
            Vector2 screenPosition = default,
            Vector2 offset = default,
            bool followPointer = false,
            float margin = 12f,
            FloatingViewAnchor preferredAnchor = FloatingViewAnchor.BottomRight,
            bool useScreenPosition = false)
        {
            valuesAssigned = true;
            this.useScreenPosition = useScreenPosition;
            this.margin = margin;
            Anchor = anchor;
            ScreenPosition = screenPosition;
            Offset = offset;
            FollowPointer = followPointer;
            PreferredAnchor = preferredAnchor;
        }

        public RectTransform Anchor { get; }
        public Vector2 ScreenPosition { get; }
        public Vector2 Offset { get; }
        public bool FollowPointer { get; }
        public float Margin => valuesAssigned ? margin : 12f;
        public FloatingViewAnchor PreferredAnchor { get; }

        public bool HasAnchor => Anchor != null;
        public bool HasScreenPosition => ScreenPosition != default || useScreenPosition;
        public static TooltipOptions Default => new TooltipOptions(margin: 12f);
    }
}
