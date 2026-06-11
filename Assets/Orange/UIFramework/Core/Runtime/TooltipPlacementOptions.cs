using UnityEngine;

namespace Orange.UIFramework
{
    public readonly struct TooltipPlacementOptions
    {
        private readonly bool valuesAssigned;
        private readonly bool useScreenPosition;
        private readonly float margin;

        public TooltipPlacementOptions(
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

        public static TooltipPlacementOptions Default => new TooltipPlacementOptions(margin: 12f);

        public TooltipPlacementOptions WithScreenPosition(Vector2 screenPosition)
        {
            return new TooltipPlacementOptions(
                Anchor,
                screenPosition,
                Offset,
                FollowPointer,
                Margin,
                PreferredAnchor,
                useScreenPosition: true);
        }

        public TooltipPlacementOptions WithoutFollowPointer()
        {
            return new TooltipPlacementOptions(
                Anchor,
                ScreenPosition,
                Offset,
                followPointer: false,
                Margin,
                PreferredAnchor,
                HasScreenPosition);
        }
    }
}
