using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class InventoryListRegionView
{
    private readonly string ownerName;
    private readonly InventoryItem itemPrefab;
    private readonly Transform itemContainersParent;
    private readonly List<InventoryItem> spawnedItems = new();

    public InventoryListRegionView(
        string ownerName,
        InventoryItem itemPrefab,
        Transform itemContainersParent)
    {
        this.ownerName = string.IsNullOrWhiteSpace(ownerName) ? nameof(InventoryListRegionView) : ownerName;
        this.itemPrefab = itemPrefab ?? throw new MissingReferenceException($"{nameof(InventoryUI)} '{this.ownerName}' is missing {nameof(InventoryItem)} prefab.");
        this.itemContainersParent = itemContainersParent ?? throw new MissingReferenceException($"{nameof(InventoryUI)} '{this.ownerName}' is missing item containers parent.");

        itemContainersParent.Clear();
    }

    public event Action<string> ItemClicked;

    public void Render(InventoryUIItemSnapshot[] items)
    {
        Clear();
        itemContainersParent.Clear();
        if (items == null || items.Length == 0)
        {
            return;
        }

        for (int i = 0; i < items.Length; i++)
        {
            SpawnItem(items[i]);
        }
    }

    public void Clear()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            InventoryItem item = spawnedItems[i];
            item.Clicked -= OnItemClicked;
            item.Dispose();
            UnityEngine.Object.Destroy(item.gameObject);
        }

        spawnedItems.Clear();
    }

    private void SpawnItem(InventoryUIItemSnapshot snapshot)
    {
        if (snapshot.ItemData == null || string.IsNullOrEmpty(snapshot.EntryId))
        {
            return;
        }

        InventoryItem item = UnityEngine.Object.Instantiate(itemPrefab, itemContainersParent);
        item.Configure(snapshot.EntryId, snapshot.ItemData, snapshot.ColorDependencyNumber);
        item.Clicked += OnItemClicked;
        spawnedItems.Add(item);
    }

    private void OnItemClicked(string entryId)
    {
        ItemClicked?.Invoke(entryId);
    }
}
