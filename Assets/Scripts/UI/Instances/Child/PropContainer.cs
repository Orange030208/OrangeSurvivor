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
        if (propText != null)
        {
            propText.ForceMeshUpdate();
        }
    }

    public void Configure(Sprite icon, string propName, float propValue)
    {
        Configure(icon, propName, propValue.ToString("F1"), propValue, true);
    }

    public void Configure(Sprite icon, string propName, string propValueTextValue, float rawValue)
    {
        Configure(icon, propName, propValueTextValue, rawValue, true);
    }

    public void Configure(Sprite icon, string propName, string propValueTextValue, float rawValue, bool useSemanticColor)
    {
        ResolveReferences();
        propImage.sprite = icon;
        propImage.enabled = icon != null;
        propText.text = propName;
        SetValue(propValueTextValue, rawValue, useSemanticColor);
    }

    public void Configure(Sprite icon, string propName, string propValueTextValue, Color valueColor)
    {
        ResolveReferences();
        propImage.sprite = icon;
        propImage.enabled = icon != null;
        propText.text = propName;
        propValueText.color = valueColor;
        propValueText.text = propValueTextValue;
    }

    public void SetValue(string displayText, float rawValue)
    {
        SetValue(displayText, rawValue, true);
    }

    public void SetValue(string displayText, float rawValue, bool useSemanticColor)
    {
        ResolveReferences();
        propValueText.color = useSemanticColor ? ColorHelper.GetColorByValue(rawValue) : Color.white;
        propValueText.text = displayText;
    }

    private void ResolveReferences()
    {
        if (propImage == null)
        {
            propImage = FindComponent<Image>("Icon");
        }

        if (propText == null)
        {
            propText = FindComponent<TextMeshProUGUI>("Name") ?? FindComponent<TextMeshProUGUI>("PropName");
        }

        if (propValueText == null)
        {
            propValueText = FindComponent<TextMeshProUGUI>("Value") ?? FindComponent<TextMeshProUGUI>("PropValue");
        }
    }

    private T FindComponent<T>(string childName) where T : Component
    {
        Transform child = FindChildByName(transform, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        if (string.Equals(root.name, childName, System.StringComparison.Ordinal))
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildByName(root.GetChild(i), childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
