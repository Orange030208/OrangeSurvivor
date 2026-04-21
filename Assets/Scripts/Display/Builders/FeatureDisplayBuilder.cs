using System.Collections.Generic;

/// <summary>
/// 将属性词条与特性效果统一转换为展示文档。
/// 作为旧字符串描述构建逻辑的替代入口。
/// </summary>
public sealed class FeatureDisplayBuilder
{
    private readonly PropEntryDisplayBuilder propEntryDisplayBuilder = new();

    public DisplayDocument Build(IReadOnlyList<PropEntry> propEntries, IReadOnlyList<FeatureEffectBase> featureEffects, DisplayContext context = null)
    {
        context ??= DisplayContext.Default;

        List<TextLineItem> textItems = new();

        TextListBlock propTextBlock = propEntryDisplayBuilder.BuildTextBlock(propEntries, context);
        if (propTextBlock != null && propTextBlock.Items != null)
        {
            for (int i = 0; i < propTextBlock.Items.Count; i++)
            {
                TextLineItem item = propTextBlock.Items[i];
                if (item == null || string.IsNullOrWhiteSpace(item.Text))
                {
                    continue;
                }

                textItems.Add(item);
            }
        }

        if (featureEffects != null)
        {
            for (int i = 0; i < featureEffects.Count; i++)
            {
                FeatureEffectBase effect = featureEffects[i];
                if (effect == null || string.IsNullOrWhiteSpace(effect.FeatureDescription))
                {
                    continue;
                }

                textItems.Add(new TextLineItem
                {
                    Text = effect.FeatureDescription,
                    StyleKey = "default"
                });
            }
        }

        return new DisplayDocument
        {
            Id = "feature_document",
            Blocks = new DisplayBlock[]
            {
                new TextListBlock
                {
                    BlockId = "feature_descriptions",
                    Header = context.IsCompact ? null : "说明",
                    Order = 0,
                    Items = textItems
                }
            }
        };
    }
}
