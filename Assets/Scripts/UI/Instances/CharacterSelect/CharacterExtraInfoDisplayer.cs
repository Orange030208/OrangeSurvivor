using TMPro;
using UnityEngine;

public class CharacterExtraInfoDisplayer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI infoRichText;

    public void DisplayInfo(IFeatureSource featureSource)
    {
        if (featureSource == null)
        {
            DisplayDescriptions(null);
            return;
        }

        var features = featureSource.GetFeatureViewData();
        if (features == null || features.Count == 0)
        {
            DisplayDescriptions(null);
            return;
        }

        string[] descriptions = new string[features.Count];
        for (int i = 0; i < features.Count; i++)
        {
            descriptions[i] = features[i].Description;
        }

        DisplayDescriptions(descriptions);
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
