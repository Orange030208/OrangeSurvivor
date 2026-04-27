using System;
using UnityEngine;

[Serializable]
public struct ItemQualityVisualStyle
{
    [Min(0)]
    [SerializeField] private int qualityValue;
    [SerializeField] private string qualityLabel;
    [SerializeField] private Color primaryColor;

    public ItemQualityVisualStyle(
        int qualityValue,
        string qualityLabel,
        Color primaryColor)
    {
        this.qualityValue = Mathf.Max(0, qualityValue);
        this.qualityLabel = qualityLabel;
        this.primaryColor = primaryColor;
    }

    public int QualityValue => qualityValue;
    public string QualityLabel => qualityLabel;
    public Color PrimaryColor => primaryColor;
}
