using Orange.UIFramework;
using System;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour, IDisposable
{
    [SerializeField] private Image iconImage;
    [SerializeField] private UIClickTarget button;
    [SerializeField] private CardQualityVisualController cardQualityVisualController;

    private string entryId;

    public event Action<string> Clicked;

    public void Configure(string entryId, ItemDataSO itemData, int colorDependencyNumber)
    {
        this.entryId = entryId;

        if (iconImage != null)
        {
            iconImage.sprite = itemData != null ? itemData.ItemIcon : null;
        }

        ApplyCardQuality(itemData, colorDependencyNumber);

        button.OnClicked -= OnItemClicked;
        button.OnClicked += OnItemClicked;
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
        button.OnClicked -= OnItemClicked;
        Clicked = null;
        entryId = null;
    }

    private void ApplyCardQuality(ItemDataSO itemData, int colorDependencyNumber)
    {
        if (cardQualityVisualController == null)
        {
            cardQualityVisualController = GetComponent<CardQualityVisualController>();
        }

        if (cardQualityVisualController == null)
        {
            return;
        }

        CardQuality quality = CardQualityResolver.FromItem(itemData, colorDependencyNumber);
        CardQualityPresentationCatalogSO catalog = ResourcesManager.GetCardQualityPresentationCatalog();
        if (catalog != null && catalog.TryGetProfile(quality, out CardQualityPresentationProfile profile))
        {
            cardQualityVisualController.Apply(profile);
        }
    }
}
