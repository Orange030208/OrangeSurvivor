using System;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PropContainer : ViewPartBase
{
    [SerializeField] private Image propImage;
    [SerializeField] private TextMeshProUGUI propText;
    [SerializeField] private TextMeshProUGUI propValueText;

    private void Awake()
    {
        propText.ForceMeshUpdate();
    }

    public void Configure(Sprite icon, string propName, float propValue)
    {
        Configure(icon, propName, propValue.ToString("F1"), propValue);
    }

    public void Configure(Sprite icon, string propName, string propValueTextValue, float rawValue)
    {
        propImage.sprite = icon;
        propText.text = propName;
        SetValue(propValueTextValue, rawValue);
    }

    public void SetValue(string displayText, float rawValue)
    {
        propValueText.color = ColorHelper.GetColorByValue(rawValue);
        propValueText.text = displayText;
    }
}
