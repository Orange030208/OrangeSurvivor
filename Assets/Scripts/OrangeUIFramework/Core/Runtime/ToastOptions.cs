using UnityEngine;

namespace Orange.UIFramework
{
    public enum ToastDisplayMode
    {
        Queue = 0,
        ReplaceCurrent = 1
    }

    public readonly struct ToastOptions
    {
        private const float DEFAULT_DURATION_SECONDS = 1.6f;
        private const float DEFAULT_MARGIN = 24f;
        private static readonly Vector2 defaultOffset = new Vector2(0f, 260f);

        private readonly bool valuesAssigned;
        private readonly bool useScreenPosition;
        private readonly float durationSeconds;
        private readonly float margin;
        private readonly Vector2 offset;

        public ToastOptions(
            float durationSeconds = DEFAULT_DURATION_SECONDS,
            RectTransform anchor = null,
            Vector2 screenPosition = default,
            Vector2 offset = default,
            float margin = DEFAULT_MARGIN,
            FloatingViewAnchor preferredAnchor = FloatingViewAnchor.Center,
            bool useScreenPosition = false,
            ToastDisplayMode displayMode = ToastDisplayMode.Queue)
        {
            valuesAssigned = true;
            this.useScreenPosition = useScreenPosition;
            this.durationSeconds = durationSeconds;
            this.margin = margin;
            DisplayMode = displayMode;
            Anchor = anchor;
            ScreenPosition = screenPosition;
            this.offset = anchor == null && screenPosition == default && offset == default && !useScreenPosition
                ? defaultOffset
                : offset;
            PreferredAnchor = preferredAnchor;
        }

        public RectTransform Anchor { get; }
        public Vector2 ScreenPosition { get; }
        public Vector2 Offset => valuesAssigned ? offset : defaultOffset;
        public float DurationSeconds => valuesAssigned ? Mathf.Max(0f, durationSeconds) : DEFAULT_DURATION_SECONDS;
        public float Margin => valuesAssigned ? Mathf.Max(0f, margin) : DEFAULT_MARGIN;
        public FloatingViewAnchor PreferredAnchor { get; }
        public ToastDisplayMode DisplayMode { get; }

        public bool HasAnchor => Anchor != null;
        public bool HasScreenPosition => ScreenPosition != default || useScreenPosition;
        public static ToastOptions Default => new ToastOptions();
    }
}
