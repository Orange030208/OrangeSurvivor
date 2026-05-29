using UnityEngine;

namespace Orange.UIFramework
{
    public readonly struct PopupOptions
    {
        private readonly bool valuesAssigned;
        private readonly bool closeOnOutsideClick;
        private readonly bool showBackdrop;
        private readonly bool trackInStack;
        private readonly bool useScreenPosition;
        private readonly float margin;

        public PopupOptions(
            RectTransform anchor = null,
            Vector2 screenPosition = default,
            Vector2 offset = default,
            bool closeOnOutsideClick = true,
            string groupId = "",
            bool replaceSameGroup = false,
            bool trackInStack = true,
            float margin = 12f,
            FloatingViewAnchor preferredAnchor = FloatingViewAnchor.BottomRight,
            bool useScreenPosition = false,
            bool showBackdrop = false)
        {
            valuesAssigned = true;
            this.closeOnOutsideClick = closeOnOutsideClick;
            this.showBackdrop = showBackdrop;
            this.trackInStack = trackInStack;
            this.useScreenPosition = useScreenPosition;
            this.margin = margin;
            Anchor = anchor;
            ScreenPosition = screenPosition;
            Offset = offset;
            GroupId = groupId ?? string.Empty;
            ReplaceSameGroup = replaceSameGroup;
            PreferredAnchor = preferredAnchor;
        }

        public RectTransform Anchor { get; }
        public Vector2 ScreenPosition { get; }
        public Vector2 Offset { get; }
        public bool CloseOnOutsideClick => !valuesAssigned || closeOnOutsideClick;
        public bool ShowBackdrop => valuesAssigned && showBackdrop;
        public bool UsesPopupBackdrop => CloseOnOutsideClick || ShowBackdrop;
        public string GroupId { get; }
        public bool ReplaceSameGroup { get; }
        public bool TrackInStack => !valuesAssigned || trackInStack;
        public float Margin => valuesAssigned ? margin : 12f;
        public FloatingViewAnchor PreferredAnchor { get; }

        public bool HasAnchor => Anchor != null;
        public bool HasScreenPosition => ScreenPosition != default || useScreenPosition;
        public static PopupOptions Default => new PopupOptions();
    }
}
