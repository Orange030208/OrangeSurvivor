namespace Orange.UIFramework
{
    public readonly struct TooltipBuildContext
    {
        public TooltipBuildContext(
            object source,
            string viewIdOverride,
            TooltipChromeOptions chromeOptions,
            TooltipSessionMode sessionMode)
        {
            Source = source;
            ViewIdOverride = viewIdOverride ?? string.Empty;
            ChromeOptions = chromeOptions;
            SessionMode = sessionMode;
        }

        public object Source { get; }
        public string ViewIdOverride { get; }
        public TooltipChromeOptions ChromeOptions { get; }
        public TooltipSessionMode SessionMode { get; }
    }
}
