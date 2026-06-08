using Orange.UIFramework;
using TMPro;
using UnityEngine;

public sealed class EquipmentInfoDocumentView : ViewPartBase
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI metaText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private GameObject iconRoot;

    private void Awake()
    {
        ResolveReferences();
    }

    public void Render(ItemInfoViewData data)
    {
        ResolveReferences();
        if (nameText != null)
        {
            nameText.text = data.Name ?? string.Empty;
        }

        if (metaText != null)
        {
            metaText.text = data.GetMetaText();
            metaText.gameObject.SetActive(!string.IsNullOrWhiteSpace(metaText.text));
        }

        if (descriptionText != null)
        {
            descriptionText.text = data.BodyRichText ?? string.Empty;
        }

        if (iconRoot != null && iconRoot.activeSelf)
        {
            iconRoot.SetActive(false);
        }
    }

    public void Clear()
    {
        Render(default);
    }

    private void ResolveReferences()
    {
        if (nameText == null)
        {
            Transform text = FindChildByName(transform, "Name")
                ?? FindChildByName(transform, "ItemName");
            nameText = text != null ? text.GetComponent<TextMeshProUGUI>() : null;
        }

        if (metaText == null)
        {
            Transform text = FindChildByName(transform, "Tag")
                ?? FindChildByName(transform, "ItemType");
            metaText = text != null ? text.GetComponent<TextMeshProUGUI>() : null;
        }

        if (descriptionText == null)
        {
            Transform text = FindChildByName(transform, "Desc")
                ?? FindChildByName(transform, "InfoText");
            descriptionText = text != null ? text.GetComponent<TextMeshProUGUI>() : null;
        }

        if (iconRoot == null)
        {
            Transform icon = FindChildByName(transform, "Icon")
                ?? FindChildByName(transform, "InfoIcon");
            iconRoot = icon != null ? icon.gameObject : null;
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
