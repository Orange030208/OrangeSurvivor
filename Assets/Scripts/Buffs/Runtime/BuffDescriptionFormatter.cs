using System.Globalization;

public static class BuffDescriptionFormatter
{
    private const string CurrentStacksToken = "{current_stacks}";
    private const string MaxStacksToken = "{max_stacks}";
    private const string RemainingDurationToken = "{remaining_duration}";
    private const string TotalDurationToken = "{total_duration}";

    public static string Format(string template, BuffDescriptionContext context)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return string.Empty;
        }

        string remainingDurationText = context.HasDuration
            ? context.RemainingDurationSeconds.ToString("0.0", CultureInfo.InvariantCulture)
            : "∞";
        string totalDurationText = context.HasDuration
            ? context.TotalDurationSeconds.ToString("0.0", CultureInfo.InvariantCulture)
            : "∞";

        return template
            .Replace(CurrentStacksToken, context.CurrentStackCount.ToString(CultureInfo.InvariantCulture))
            .Replace(MaxStacksToken, context.MaxStackCount.ToString(CultureInfo.InvariantCulture))
            .Replace(RemainingDurationToken, remainingDurationText)
            .Replace(TotalDurationToken, totalDurationText);
    }
}
