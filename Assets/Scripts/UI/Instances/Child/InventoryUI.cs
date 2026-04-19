using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("容器与预制体")]
    [SerializeField] private InventoryItemContainer itemContainerPrefab;
    [SerializeField] private InventoryItemOperateContainer inventoryItemOperateContainer;
    [SerializeField] private UISidebarRevealMotion inventoryItemOperateContainerSidebar;
    [SerializeField] private Transform itemContainersParent;

    [SerializeField] private UIClickTarget[] closeInventoryItemOperatePanelButtons;

    private readonly List<InventoryItemContainer> spawnedContainers = new();
    private bool subscribed;
    private int currentOperateItemIndex = -1;

    private void Awake()
    {
        if (itemContainerPrefab == null)
        {
            throw new MissingReferenceException($"{nameof(InventoryUI)} '{name}' is missing {nameof(InventoryItemContainer)} prefab.");
        }

        if (inventoryItemOperateContainer == null)
        {
            throw new MissingReferenceException($"{nameof(InventoryUI)} '{name}' is missing {nameof(InventoryItemOperateContainer)}.");
        }

        if (inventoryItemOperateContainerSidebar == null)
        {
            throw new MissingReferenceException($"{nameof(InventoryUI)} '{name}' is missing {nameof(UISidebarRevealMotion)}.");
        }

        if (itemContainersParent == null)
        {
            throw new MissingReferenceException($"{nameof(InventoryUI)} '{name}' is missing item containers parent.");
        }

        if (closeInventoryItemOperatePanelButtons == null)
        {
            throw new MissingReferenceException($"{nameof(InventoryUI)} '{name}' is missing close panel buttons.");
        }
    }

    private void OnEnable()
    {
        Subscribe();
        RefreshOperatePanelSidebar();
        CloseOperatePanelImmediate();
        GameEventBus.Publish(new RequestInventorySnapshotEvent());
    }

    private void OnDisable()
    {
        Unsubscribe();
        CleanupSpawnedContainers();
        inventoryItemOperateContainer.Dispose();
        inventoryItemOperateContainerSidebar.Kill();
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
        CleanupSpawnedContainers();
        itemContainersParent.Clear();

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
        currentOperateItemIndex = eventData.Resource.itemIndex;
        inventoryItemOperateContainer.Configure(eventData.Resource);
        inventoryItemOperateContainerSidebar.Play(UIMotionAction.Show);
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
        foreach (UIClickTarget item in closeInventoryItemOperatePanelButtons)
        {
            item.OnClicked += OnClosePanelBackgroundClicked;
        }
    }

    private void UnbindClosePanelHandlers()
    {
        foreach (UIClickTarget item in closeInventoryItemOperatePanelButtons)
        {
            item.OnClicked -= OnClosePanelBackgroundClicked;
        }
    }

    private void OnClosePanelBackgroundClicked()
    {
        CloseOperatePanel();
    }

    private void CloseOperatePanel()
    {
        currentOperateItemIndex = -1;
        inventoryItemOperateContainer.Dispose();
        inventoryItemOperateContainerSidebar.Play(UIMotionAction.Hide);
    }

    private void CloseOperatePanelImmediate()
    {
        currentOperateItemIndex = -1;
        inventoryItemOperateContainer.Dispose();
        inventoryItemOperateContainerSidebar.SetImmediate(UIMotionAction.Hide);
    }

    private void CleanupSpawnedContainers()
    {
        for (int i = 0; i < spawnedContainers.Count; i++)
        {
            spawnedContainers[i].Dispose();
        }

        spawnedContainers.Clear();
    }

    private void RefreshOperatePanelSidebar()
    {
        inventoryItemOperateContainerSidebar.RefreshDefaults();
    }
}
