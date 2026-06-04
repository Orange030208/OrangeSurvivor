using System;

namespace Orange.UIFramework
{
    public readonly struct TooltipRequest
    {
        public TooltipRequest(
            object source = null,
            TooltipContent content = null,
            TooltipPlacementOptions placementOptions = default,
            TooltipPinMode pinMode = TooltipPinMode.Disabled,
            TooltipChromeOptions chromeOptions = default,
            string viewIdOverride = "",
            TooltipSessionMode sessionMode = TooltipSessionMode.Transient)
        {
            Source = source;
            Content = content;
            PlacementOptions = placementOptions;
            PinMode = pinMode;
            ChromeOptions = chromeOptions;
            ViewIdOverride = viewIdOverride ?? string.Empty;
            SessionMode = pinMode == TooltipPinMode.Pinned ? TooltipSessionMode.Pinned : sessionMode;
        }

        public object Source { get; }
        public TooltipContent Content { get; }
        public TooltipPlacementOptions PlacementOptions { get; }
        public TooltipPinMode PinMode { get; }
        public TooltipChromeOptions ChromeOptions { get; }
        public string ViewIdOverride { get; }
        public TooltipSessionMode SessionMode { get; }

        public object ResolveSource()
        {
            return Content != null ? Content : Source;
        }

        public TooltipRequest WithScreenPosition(UnityEngine.Vector2 screenPosition)
        {
            return new TooltipRequest(
                Source,
                Content,
                PlacementOptions.WithScreenPosition(screenPosition),
                PinMode,
                ChromeOptions,
                ViewIdOverride,
                SessionMode);
        }

        public void Validate()
        {
            if (Content == null && Source == null)
            {
                throw new InvalidOperationException("TooltipRequest requires either Content or Source.");
            }
        }
    }
}
