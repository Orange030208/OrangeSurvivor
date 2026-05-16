#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;

public sealed class DataImportReport
{
    private readonly List<string> createdAssets = new();
    private readonly List<string> updatedAssets = new();
    private readonly List<string> deleteCandidates = new();
    private readonly List<string> warnings = new();
    private readonly List<string> blockers = new();

    public IReadOnlyList<string> CreatedAssets => createdAssets;
    public IReadOnlyList<string> UpdatedAssets => updatedAssets;
    public IReadOnlyList<string> DeleteCandidates => deleteCandidates;
    public IReadOnlyList<string> Warnings => warnings;
    public IReadOnlyList<string> Blockers => blockers;
    public bool HasBlockers => blockers.Count > 0;

    public void AddCreated(string value) => Add(createdAssets, value);
    public void AddUpdated(string value) => Add(updatedAssets, value);
    public void AddDeleteCandidate(string value) => Add(deleteCandidates, value);
    public void AddWarning(string value) => Add(warnings, value);
    public void AddBlocker(string value) => Add(blockers, value);

    public string ToSummary()
    {
        return $"created={createdAssets.Count}, updated={updatedAssets.Count}, deleteCandidates={deleteCandidates.Count}, warnings={warnings.Count}, blockers={blockers.Count}";
    }

    public string ToMarkdown()
    {
        StringBuilder builder = new();
        builder.AppendLine("# JSON 数据导入报告");
        builder.AppendLine();
        builder.AppendLine($"摘要：{ToSummary()}");
        builder.AppendLine();
        AppendSection(builder, "阻塞项", blockers);
        AppendSection(builder, "新增资产", createdAssets);
        AppendSection(builder, "更新资产", updatedAssets);
        AppendSection(builder, "删除候选", deleteCandidates);
        AppendSection(builder, "警告", warnings);
        return builder.ToString();
    }

    private static void AppendSection(StringBuilder builder, string title, IReadOnlyList<string> values)
    {
        builder.AppendLine($"## {title}");
        if (values.Count == 0)
        {
            builder.AppendLine("- 无");
            builder.AppendLine();
            return;
        }

        for (int i = 0; i < values.Count; i++)
        {
            builder.AppendLine($"- {values[i]}");
        }

        builder.AppendLine();
    }

    private static void Add(List<string> target, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            target.Add(value);
        }
    }
}
#endif
