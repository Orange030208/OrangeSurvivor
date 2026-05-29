using Orange.UIFramework;
using System;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItem : ViewPartBase, IDisposable
{
    [SerializeField] private Image frameImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button button;

    private string entryId;

    public event Action<string> Clicked;

    public void Configure(string entryId, ItemDataSO itemData, int colorDependencyNumber)
    {
        ResolveReferences();
        this.entryId = entryId;

        if (iconImage != null)
        {
            iconImage.sprite = itemData != null ? itemData.ItemIcon : null;
        }

        ApplyTier(ContentTierResolver.FromItem(itemData, colorDependencyNumber));

        if (button == null)
        {
            Debug.LogWarning($"{nameof(InventoryItem)} '{name}' is missing button reference.", this);
            return;
        }

        button.onClick.RemoveListener(OnItemClicked);
        button.onClick.AddListener(OnItemClicked);
    }

    public void Configure(string entryId, ItemDataSO itemData, IHasContentTier tierSource)
    {
        ResolveReferences();
        this.entryId = entryId;

        if (iconImage != null)
        {
            iconImage.sprite = itemData != null ? itemData.ItemIcon : null;
        }

        ApplyTier(tierSource);

        if (button == null)
        {
            Debug.LogWarning($"{nameof(InventoryItem)} '{name}' is missing button reference.", this);
            return;
        }

        button.onClick.RemoveListener(OnItemClicked);
        button.onClick.AddListener(OnItemClicked);
    }

    private void OnItemClicked()
    {
        if (string.IsNullOrEmpty(entryId))
        {
            return;
        }

        Clicked?.Invoke(entryId);
    }

    public void Dispose()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnItemClicked);
        }

        Clicked = null;
        entryId = null;
    }

    private void ApplyTier(IHasContentTier tierSource)
    {
        ResolveReferences();
        if (frameImage != null && tierSource != null)
        {
            frameImage.color = ResolveFallbackColor(tierSource.Tier);
        }
    }

    private void ApplyTier(ContentTier tier)
    {
        ResolveReferences();
        if (frameImage != null)
        {
            frameImage.color = ResolveFallbackColor(tier);
        }
    }

    private static Color ResolveFallbackColor(ContentTier tier)
    {
        return tier switch
        {
            ContentTier.Rare => new Color32(77, 140, 255, 255),
            ContentTier.Epic => new Color32(163, 104, 255, 255),
            ContentTier.Legendary => new Color32(255, 166, 52, 255),
            _ => new Color32(172, 172, 172, 255)
        };
    }

    private void ResolveReferences()
    {
        if (frameImage == null)
        {
            frameImage = FindImageByName("Frame");
        }

        if (iconImage == null)
        {
            iconImage = FindImageByName("Icon");
        }

        if (button == null)
        {
            button = GetComponent<Button>() ?? GetComponentInChildren<Button>(true);
        }
    }

    private Image FindImageByName(string targetName)
    {
        Transform target = FindChildByName(transform, targetName);
        return target != null ? target.GetComponent<Image>() : null;
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
