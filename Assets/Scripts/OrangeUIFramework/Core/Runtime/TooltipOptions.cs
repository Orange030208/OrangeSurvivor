using UnityEngine;

namespace Orange.UIFramework
{
    public readonly struct TooltipOptions
    {
        private readonly bool valuesAssigned;
        private readonly float margin;

        public TooltipOptions(
            RectTransform anchor = null,
            Vector2 screenPosition = default,
            Vector2 offset = default,
            bool followPointer = false,
            float margin = 12f)
        {
            valuesAssigned = true;
            this.margin = margin;
            Anchor = anchor;
            ScreenPosition = screenPosition;
            Offset = offset;
            FollowPointer = followPointer;
        }

        public RectTransform Anchor { get; }
        public Vector2 ScreenPosition { get; }
        public Vector2 Offset { get; }
        public bool FollowPointer { get; }
        public float Margin => valuesAssigned ? margin : 12f;

        public bool HasAnchor => Anchor != null;
        public bool HasScreenPosition => ScreenPosition != default;
        public static TooltipOptions Default => new TooltipOptions(margin: 12f);
    }
}
