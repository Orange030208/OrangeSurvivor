using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public sealed class EquipmentInfoDocumentView : ViewPartBase
{
    [FormerlySerializedAs("iconImage")]
    [SerializeField] private Image legacyIconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [FormerlySerializedAs("typeText")]
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

        if (iconRoot == null && legacyIconImage != null)
        {
            iconRoot = legacyIconImage.gameObject;
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

        if (legacyIconImage == null)
        {
            Transform icon = FindChildByName(transform, "Icon")
                ?? FindChildByName(transform, "InfoIcon");
            legacyIconImage = icon != null ? icon.GetComponent<Image>() : null;
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
