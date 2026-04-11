using TMPro;
using UnityEngine;

public class DescriptionListDisplayer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI infoRichText;

    public void DisplaySource(IDescriptionSource descriptionSource)
    {
        if (descriptionSource == null)
        {
            DisplayDescriptions(null);
            return;
        }

        DisplayDescriptions(descriptionSource.GetDescriptions());
    }

    public void DisplayDescriptions(System.Collections.Generic.IReadOnlyList<string> descriptions)
    {
        if (infoRichText == null)
        {
            return;
        }

        if (descriptions == null || descriptions.Count == 0)
        {
            infoRichText.text = "暂无特殊特性";
            return;
        }

        infoRichText.richText = true;

        System.Text.StringBuilder builder = new();
        for (int i = 0; i < descriptions.Count; i++)
        {
            if (string.IsNullOrEmpty(descriptions[i]))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(descriptions[i]);
        }

        infoRichText.text = builder.Length > 0 ? builder.ToString() : "暂无特殊特性";
    }
}
