using System;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour, IDisposable
{
    [SerializeField] private Image iconImage;
    [SerializeField] private UIClickTarget button;

    private string entryId;
    private ItemDataSO currentItemData;
    private int currentColorDependencyNumber;

    public event Action<string> Clicked;

    public void Configure(string entryId, ItemDataSO itemData, int colorDependencyNumber)
    {
        this.entryId = entryId;
        currentItemData = itemData;
        currentColorDependencyNumber = colorDependencyNumber;

        ItemQualityVisualResolver.Apply(this, itemData, colorDependencyNumber, iconImage);

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
        currentItemData = null;
        currentColorDependencyNumber = 0;
        entryId = null;
    }
}
