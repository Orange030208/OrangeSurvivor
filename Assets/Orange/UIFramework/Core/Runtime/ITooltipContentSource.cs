namespace Orange.UIFramework
{
    public interface ITooltipContentSource
    {
        bool TryBuildTooltipContent(out TooltipContent content);
    }
}
