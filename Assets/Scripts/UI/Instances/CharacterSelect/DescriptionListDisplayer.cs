using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DescriptionListDisplayer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI infoRichText;

    public void Display(DisplayDocument document)
    {
        if (document == null)
        {
            Display((TextListBlock)null);
            return;
        }

        Display(document.GetBlock<TextListBlock>());
    }

    public void Display(TextListBlock block)
    {
        if (infoRichText == null)
        {
            return;
        }

        IReadOnlyList<TextLineItem> items = block != null ? block.Items : null;
        if (items == null || items.Count == 0)
        {
            infoRichText.text = "暂无特殊特性";
            return;
        }

        infoRichText.richText = true;

        System.Text.StringBuilder builder = new();
        for (int i = 0; i < items.Count; i++)
        {
            TextLineItem item = items[i];
            if (item == null || string.IsNullOrWhiteSpace(item.Text))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(item.Text);
        }

        infoRichText.text = builder.Length > 0 ? builder.ToString() : "暂无特殊特性";
    }
}
