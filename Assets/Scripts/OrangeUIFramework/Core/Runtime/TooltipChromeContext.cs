namespace Orange.UIFramework
{
    public readonly struct TooltipChromeContext
    {
        public TooltipChromeContext(
            TooltipSessionHandle sessionHandle,
            TooltipChromeOptions chromeOptions,
            TooltipSessionMode sessionMode)
        {
            SessionHandle = sessionHandle;
            ChromeOptions = chromeOptions;
            SessionMode = sessionMode;
        }

        public TooltipSessionHandle SessionHandle { get; }
        public TooltipChromeOptions ChromeOptions { get; }
        public TooltipSessionMode SessionMode { get; }
        public bool IsPinned => SessionMode == TooltipSessionMode.Pinned;
    }
}
