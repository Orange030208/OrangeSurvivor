using System;
using UnityEngine;

[Serializable]
public struct ItemQualityVisualStyle
{
    [Min(0)]
    [SerializeField] private int qualityValue;
    [SerializeField] private string qualityLabel;
    [SerializeField] private Color primaryColor;
    [SerializeField] private Color secondaryColor;
    [SerializeField] private Color nameColor;
    [SerializeField] private Color glowColor;
    [SerializeField] private bool enablePremiumEffect;
    [SerializeField] private bool enablePulseEffect;

    public ItemQualityVisualStyle(
        int qualityValue,
        string qualityLabel,
        Color primaryColor,
        Color secondaryColor,
        Color nameColor,
        Color glowColor,
        bool enablePremiumEffect,
        bool enablePulseEffect)
    {
        this.qualityValue = Mathf.Max(0, qualityValue);
        this.qualityLabel = qualityLabel;
        this.primaryColor = primaryColor;
        this.secondaryColor = secondaryColor;
        this.nameColor = nameColor;
        this.glowColor = glowColor;
        this.enablePremiumEffect = enablePremiumEffect;
        this.enablePulseEffect = enablePulseEffect;
    }

    public int QualityValue => qualityValue;
    public string QualityLabel => qualityLabel;
    public Color PrimaryColor => primaryColor;
    public Color SecondaryColor => secondaryColor;
    public Color NameColor => nameColor;
    public Color GlowColor => glowColor;
    public bool EnablePremiumEffect => enablePremiumEffect;
    public bool EnablePulseEffect => enablePulseEffect;

    public ItemQualityVisualStyle WithFallbacks()
    {
        Color resolvedSecondaryColor = HasVisibleAlpha(secondaryColor) ? secondaryColor : primaryColor;
        Color resolvedNameColor = HasVisibleAlpha(nameColor) ? nameColor : primaryColor;
        Color resolvedGlowColor = HasVisibleAlpha(glowColor) ? glowColor : primaryColor;

        return new ItemQualityVisualStyle(
            qualityValue,
            qualityLabel,
            primaryColor,
            resolvedSecondaryColor,
            resolvedNameColor,
            resolvedGlowColor,
            enablePremiumEffect,
            enablePulseEffect);
    }

    private static bool HasVisibleAlpha(Color color)
    {
        return color.a > 0.001f;
    }
}
