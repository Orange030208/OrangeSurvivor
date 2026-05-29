using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EquipmentSelectedDisplayView : ViewPartBase
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;

    private void Awake()
    {
        ResolveReferences();
    }

    public void Render(EquipmentSelectedDisplayData data)
    {
        ResolveReferences();
        if (iconImage != null)
        {
            iconImage.sprite = data.Icon;
            iconImage.enabled = data.Icon != null;
        }

        if (nameText != null)
        {
            nameText.text = data.Name ?? string.Empty;
        }
    }

    public void Clear()
    {
        Render(default);
    }

    private void ResolveReferences()
    {
        if (iconImage == null)
        {
            Transform icon = FindChildByName(transform, "SelectedIcon");
            iconImage = icon != null ? icon.GetComponent<Image>() : null;
        }

        if (nameText == null)
        {
            Transform text = FindChildByName(transform, "SelectedName");
            nameText = text != null ? text.GetComponent<TextMeshProUGUI>() : null;
        }
    }

    private static Transform FindChildByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        if (root.name == targetName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), targetName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
