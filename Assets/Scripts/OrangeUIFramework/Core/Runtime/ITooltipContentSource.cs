namespace Orange.UIFramework
{
    public interface ITooltipContentSource
    {
        bool TryBuildTooltipContent(TooltipBuildContext context, out TooltipContent content);
    }
}
