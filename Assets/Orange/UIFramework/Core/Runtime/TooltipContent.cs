using System;

namespace Orange.UIFramework
{
    public sealed class TooltipContent
    {
        public TooltipContent(
            string viewId,
            object payload,
            TooltipChromeOptions chromeOptions = default)
        {
            if (string.IsNullOrWhiteSpace(viewId))
            {
                throw new ArgumentException("Tooltip content requires a non-empty view id.", nameof(viewId));
            }

            ViewId = viewId;
            Payload = payload;
            ChromeOptions = chromeOptions;
        }

        public string ViewId { get; }
        public object Payload { get; }
        public TooltipChromeOptions ChromeOptions { get; }

        public TooltipContent WithViewId(string viewId)
        {
            return string.Equals(ViewId, viewId, StringComparison.Ordinal)
                ? this
                : new TooltipContent(viewId, Payload, ChromeOptions);
        }

        public TooltipContent WithChromeOptions(TooltipChromeOptions chromeOptions)
        {
            return new TooltipContent(ViewId, Payload, ChromeOptions.Merge(chromeOptions));
        }
    }
}
