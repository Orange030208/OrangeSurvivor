using UnityEngine;

namespace Orange.UIFramework
{
    public readonly struct PopupOptions
    {
        private readonly bool valuesAssigned;
        private readonly bool closeOnOutsideClick;
        private readonly bool trackInStack;

        public PopupOptions(
            RectTransform anchor = null,
            Vector2 screenPosition = default,
            Vector2 offset = default,
            bool closeOnOutsideClick = true,
            string groupId = "",
            bool replaceSameGroup = false,
            bool trackInStack = true)
        {
            valuesAssigned = true;
            this.closeOnOutsideClick = closeOnOutsideClick;
            this.trackInStack = trackInStack;
            Anchor = anchor;
            ScreenPosition = screenPosition;
            Offset = offset;
            GroupId = groupId ?? string.Empty;
            ReplaceSameGroup = replaceSameGroup;
        }

        public RectTransform Anchor { get; }
        public Vector2 ScreenPosition { get; }
        public Vector2 Offset { get; }
        public bool CloseOnOutsideClick => !valuesAssigned || closeOnOutsideClick;
        public string GroupId { get; }
        public bool ReplaceSameGroup { get; }
        public bool TrackInStack => !valuesAssigned || trackInStack;

        public bool HasAnchor => Anchor != null;
        public bool HasScreenPosition => ScreenPosition != default;
        public static PopupOptions Default => new PopupOptions();
    }
}
