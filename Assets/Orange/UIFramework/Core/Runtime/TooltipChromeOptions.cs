namespace Orange.UIFramework
{
    public readonly struct TooltipChromeOptions
    {
        private readonly bool valuesAssigned;

        public TooltipChromeOptions(
            bool allowUserPin = false,
            bool showCloseButton = false,
            bool allowInteractiveTransient = false)
        {
            valuesAssigned = true;
            AllowUserPin = allowUserPin;
            ShowCloseButton = showCloseButton;
            AllowInteractiveTransient = allowInteractiveTransient;
        }

        public bool AllowUserPin { get; }
        public bool ShowCloseButton { get; }
        public bool AllowInteractiveTransient { get; }
        public bool HasAssignedValues => valuesAssigned;
        public bool RequiresInteraction => AllowUserPin || ShowCloseButton || AllowInteractiveTransient;

        public static TooltipChromeOptions Passive => new TooltipChromeOptions();

        public static TooltipChromeOptions Pinnable =>
            new TooltipChromeOptions(allowUserPin: true, showCloseButton: false, allowInteractiveTransient: true);

        public TooltipChromeOptions Merge(TooltipChromeOptions overrideOptions)
        {
            if (!overrideOptions.valuesAssigned)
            {
                return this;
            }

            return overrideOptions;
        }
    }
}
