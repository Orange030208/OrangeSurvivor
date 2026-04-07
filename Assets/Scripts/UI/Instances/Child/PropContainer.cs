using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PropContainer : MonoBehaviour
{
    [SerializeField] private Image propImage;
    [SerializeField] private TextMeshProUGUI propText;
    [SerializeField] private TextMeshProUGUI propValueText;
    [SerializeField] private float propTextMaxSize=30;
    [SerializeField] private float propTextMinSize=30;
    [SerializeField] private float propValueMaxSize=30;
    [SerializeField] private float propValueMinSize=30;

    private void Awake()
    {
        propText.fontSizeMax = propTextMaxSize;
        propText.fontSizeMin = propTextMinSize;
        propValueText.fontSizeMax = propValueMaxSize;
        propValueText.fontSizeMin = propValueMinSize;
    }

    public void Configure(Sprite icon,string propName,float propValue)
    {
        propImage.sprite = icon;
        propText.text = propName;
        SetValue(propValue);
    }

    public void SetValue(float propValue)
    {
        propValueText.color = ColorHelper.GetColorByValue(propValue);
        propValueText.text = propValue.ToString("F1");
    }

    public float GetFontSize()
    {
        return propText.fontSize;
    }

    public void SetFontSize(float fontSize)
    {
        propText.fontSizeMax = fontSize;
        propText.fontSizeMin = fontSize;
        propValueText.fontSizeMax = fontSize;
        propValueText.fontSizeMin = fontSize;
    }
}