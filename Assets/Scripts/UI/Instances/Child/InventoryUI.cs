using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryUI : MonoBehaviour
{
    [Header("容器与预制体")]
    [SerializeField] private InventoryItemContainer itemContainerPrefab;
    [SerializeField] private InventoryItemOperateContainer inventoryItemOperateContainer;
    [SerializeField] private SidebarSlider inventoryItemOperateContainerSidebar;
    [SerializeField] private Transform itemContainersParent;

    [SerializeField] private ClickOnlyHandler[] closeInventoryItemOperatePanelHandlers;

    private readonly List<InventoryItemContainer> spawnedContainers = new();
    private bool subscribed;
    private int currentOperateItemIndex = -1;

    private void OnEnable()
    {
        Subscribe();
        CloseOperatePanelImmediate();
        GameEventBus.Publish(new RequestInventorySnapshotEvent());
    }

    private void OnDisable()
    {
        Unsubscribe();
        CleanupSpawnedContainers();
        inventoryItemOperateContainer?.Cleanup();
    }

    private void Subscribe()
    {
        if (subscribed)
        {
            return;
        }

        GameEventBus.Subscribe<InventorySnapshotChangedEvent>(OnInventorySnapshotChanged);
        GameEventBus.Subscribe<InventoryItemClickedEvent>(OnInventoryItemClicked);
        GameEventBus.Subscribe<InventoryItemOperatePanelDataEvent>(OnOperatePanelDataChanged);
        GameEventBus.Subscribe<InventoryItemOperatePanelShouldCloseEvent>(OnOperatePanelShouldClose);
        GameEventBus.Subscribe<InventoryItemOperatePanelCloseClickedEvent>(OnOperatePanelCloseClicked);

        BindClosePanelHandlers();

        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
        {
            return;
        }

        GameEventBus.Unsubscribe<InventorySnapshotChangedEvent>(OnInventorySnapshotChanged);
        GameEventBus.Unsubscribe<InventoryItemClickedEvent>(OnInventoryItemClicked);
        GameEventBus.Unsubscribe<InventoryItemOperatePanelDataEvent>(OnOperatePanelDataChanged);
        GameEventBus.Unsubscribe<InventoryItemOperatePanelShouldCloseEvent>(OnOperatePanelShouldClose);
        GameEventBus.Unsubscribe<InventoryItemOperatePanelCloseClickedEvent>(OnOperatePanelCloseClicked);

        UnbindClosePanelHandlers();

        subscribed = false;
    }

    private void OnInventorySnapshotChanged(InventorySnapshotChangedEvent eventData)
    {
        if (itemContainersParent == null)
        {
            return;
        }

        CleanupSpawnedContainers();
        itemContainersParent.Clear();

        if (itemContainerPrefab == null || eventData.Items == null)
        {
            return;
        }

        for (int i = 0; i < eventData.Items.Length; i++)
        {
            InventoryUIItemSnapshot item = eventData.Items[i];
            if (item.ItemData == null)
            {
                continue;
            }

            InventoryItemContainer container = Instantiate(itemContainerPrefab, itemContainersParent);
            container.Configure(item.ItemData, item.ColorDependencyNumber, i);
            spawnedContainers.Add(container);
        }
    }

    private void OnInventoryItemClicked(InventoryItemClickedEvent eventData)
    {
        GameEventBus.Publish(new RequestInventoryItemOperatePanelEvent(eventData.ItemIndex));
    }

    private void OnOperatePanelDataChanged(InventoryItemOperatePanelDataEvent eventData)
    {
        if (inventoryItemOperateContainer == null)
        {
            return;
        }

        currentOperateItemIndex = eventData.Resource.itemIndex;
        inventoryItemOperateContainer.Configure(eventData.Resource);
        inventoryItemOperateContainerSidebar?.Show();
    }

    private void OnOperatePanelShouldClose(InventoryItemOperatePanelShouldCloseEvent eventData)
    {
        if (eventData.ItemIndex != currentOperateItemIndex)
        {
            return;
        }

        CloseOperatePanel();
    }

    private void OnOperatePanelCloseClicked(InventoryItemOperatePanelCloseClickedEvent eventData)
    {
        if (eventData.ItemIndex != currentOperateItemIndex)
        {
            return;
        }

        CloseOperatePanel();
    }

    private void BindClosePanelHandlers()
    {
        if (closeInventoryItemOperatePanelHandlers == null)
        {
            return;
        }

        foreach (var item in closeInventoryItemOperatePanelHandlers)
        {
            if (item == null)
            {
                continue;
            }

            item.OnClick += OnClosePanelBackgroundClicked;
        }
    }

    private void UnbindClosePanelHandlers()
    {
        if (closeInventoryItemOperatePanelHandlers == null)
        {
            return;
        }

        foreach (var item in closeInventoryItemOperatePanelHandlers)
        {
            if (item == null)
            {
                continue;
            }

            item.OnClick -= OnClosePanelBackgroundClicked;
        }
    }

    private void OnClosePanelBackgroundClicked(PointerEventData _)
    {
        CloseOperatePanel();
    }

    private void CloseOperatePanel()
    {
        currentOperateItemIndex = -1;
        inventoryItemOperateContainer?.Cleanup();
        inventoryItemOperateContainerSidebar?.Hide();
    }

    private void CloseOperatePanelImmediate()
    {
        currentOperateItemIndex = -1;
        inventoryItemOperateContainer?.Cleanup();
        inventoryItemOperateContainerSidebar?.HideImmediate();
    }

    private void CleanupSpawnedContainers()
    {
        for (int i = 0; i < spawnedContainers.Count; i++)
        {
            if (spawnedContainers[i] != null)
            {
                spawnedContainers[i].Dispose();
            }
        }

        spawnedContainers.Clear();
    }
}
