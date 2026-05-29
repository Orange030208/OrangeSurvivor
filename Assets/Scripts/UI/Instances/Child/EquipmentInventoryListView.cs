using System;
using System.Collections.Generic;
using Orange.UIFramework;
using UnityEngine;
using UnityEngine.UI;

public sealed class EquipmentInventoryListView : ViewPartBase
{
    private const string DEFAULT_ITEM_PREFAB_PATH = "Assets/GameContent/UI/Prefabs/Equipment/EquipmentItem.prefab";

    [SerializeField] private InventoryItem itemPrefab;
    [SerializeField] private Transform itemParent;

    private readonly List<InventoryItem> renderedItems = new();

    public event Action<string> ItemClicked;

    private void Awake()
    {
        ResolveReferences();
        ValidateConfiguration();
        Clear();
    }

    private void OnDestroy()
    {
        Clear();
        ItemClicked = null;
    }

    public void Render(IReadOnlyList<EquipmentListItemViewData> items)
    {
        ResolveReferences();
        ValidateConfiguration();
        Clear();
        if (items == null)
        {
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            EquipmentListItemViewData item = items[i];
            if (item.ItemData == null || string.IsNullOrWhiteSpace(item.EntryId))
            {
                continue;
            }

            InventoryItem renderedItem = Instantiate(itemPrefab, itemParent);
            renderedItem.Configure(item.EntryId, item.ItemData, item.TierSource);
            renderedItem.Clicked += OnItemClicked;
            renderedItems.Add(renderedItem);
        }
    }

    public void Clear()
    {
        ResolveReferences();
        for (int i = 0; i < renderedItems.Count; i++)
        {
            DestroyItem(renderedItems[i]);
        }

        renderedItems.Clear();

        if (itemParent == null)
        {
            return;
        }

        for (int i = itemParent.childCount - 1; i >= 0; i--)
        {
            Destroy(itemParent.GetChild(i).gameObject);
        }
    }

    private void OnItemClicked(string entryId)
    {
        if (string.IsNullOrWhiteSpace(entryId))
        {
            return;
        }

        ItemClicked?.Invoke(entryId);
    }

    private void DestroyItem(InventoryItem item)
    {
        if (item == null)
        {
            return;
        }

        item.Clicked -= OnItemClicked;
        item.Dispose();
        Destroy(item.gameObject);
    }

    private void ResolveReferences()
    {
        if (itemParent == null)
        {
            itemParent = ResolveItemParent();
        }

#if UNITY_EDITOR
        if (itemPrefab == null)
        {
            itemPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<InventoryItem>(DEFAULT_ITEM_PREFAB_PATH);
        }
#endif
    }

    private Transform ResolveItemParent()
    {
        Transform root = transform.Find("VisualRoot/Scroll View/Viewport/Content")
            ?? transform.Find("Scroll View/Viewport/Content");
        if (root != null)
        {
            return root;
        }

        GridLayoutGroup[] grids = GetComponentsInChildren<GridLayoutGroup>(true);
        for (int i = 0; i < grids.Length; i++)
        {
            if (grids[i] != null && grids[i].enabled)
            {
                return grids[i].transform;
            }
        }

        return grids.Length > 0 ? grids[0].transform : null;
    }

    private void ValidateConfiguration()
    {
        if (itemPrefab == null)
        {
            throw new MissingReferenceException($"{nameof(EquipmentInventoryListView)} '{name}' is missing item prefab.");
        }

        if (itemParent == null)
        {
            throw new MissingReferenceException($"{nameof(EquipmentInventoryListView)} '{name}' is missing item parent.");
        }
    }
}
