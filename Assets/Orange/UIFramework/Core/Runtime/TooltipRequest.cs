using System;

namespace Orange.UIFramework
{
    public readonly struct TooltipRequest
    {
        public TooltipRequest(
            TooltipContent content = null,
            TooltipPlacementOptions placementOptions = default,
            TooltipPinMode pinMode = TooltipPinMode.Disabled,
            TooltipChromeOptions chromeOptions = default,
            TooltipSessionMode sessionMode = TooltipSessionMode.Transient)
        {
            Content = content;
            PlacementOptions = placementOptions;
            PinMode = pinMode;
            ChromeOptions = chromeOptions;
            SessionMode = pinMode == TooltipPinMode.Pinned ? TooltipSessionMode.Pinned : sessionMode;
        }

        public TooltipContent Content { get; }
        public TooltipPlacementOptions PlacementOptions { get; }
        public TooltipPinMode PinMode { get; }
        public TooltipChromeOptions ChromeOptions { get; }
        public TooltipSessionMode SessionMode { get; }

        public TooltipRequest WithScreenPosition(UnityEngine.Vector2 screenPosition)
        {
            return new TooltipRequest(
                Content,
                PlacementOptions.WithScreenPosition(screenPosition),
                PinMode,
                ChromeOptions,
                SessionMode);
        }

        public void Validate()
        {
            if (Content == null)
            {
                throw new InvalidOperationException("TooltipRequest requires Content.");
            }
        }
    }
}
