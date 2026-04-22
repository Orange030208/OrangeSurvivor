using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("容器与预制体")]
    [SerializeField] private InventoryItem itemPrefab;
    [SerializeField] private WeaponOperatePopup weaponPopupPrefab;
    [SerializeField] private AccessoryInfoPopup accessoryPopupPrefab;
    [SerializeField] private Transform itemContainersParent;

    [Header("关闭")]
    [SerializeField] private UIClickTarget[] closeInventoryItemOperatePanelButtons;

    private readonly List<InventoryItem> spawnedContainers = new();
    private readonly List<UIClickTarget> popupCloseButtons = new();
    private bool subscribed;
    private int currentOperateItemIndex = -1;
    private Transform popupLayerRoot;
    private GameObject popupCloseMask;

    private WeaponOperatePopup weaponPopupInstance;
    private AccessoryInfoPopup accessoryPopupInstance;

    private void Awake()
    {
        ValidateConfiguration();
        popupLayerRoot = ResolvePopupLayerRoot();
        EnsureFullScreenCloseMask();
    }

    private void OnEnable()
    {
        Subscribe();
        HideCloseMask();
        GameEventBus.Publish(new RequestInventorySnapshotEvent());
    }

    private void Update()
    {
        if (currentOperateItemIndex < 0)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
            CloseOperatePanel();
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
        CleanupSpawnedContainers();
        DestroyCurrentPopupImmediate();
        HideCloseMask();
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

            InventoryItem container = Instantiate(itemPrefab, itemContainersParent);
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
        if (eventData.Resource.itemData == null)
        {
            return;
        }

        currentOperateItemIndex = eventData.Resource.itemIndex;
        ShowCloseMask();
        RebuildPopup(eventData.Resource);
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
        for (int i = 0; i < popupCloseButtons.Count; i++)
        {
            popupCloseButtons[i].OnClicked += OnClosePanelBackgroundClicked;
        }
    }

    private void UnbindClosePanelHandlers()
    {
        for (int i = 0; i < popupCloseButtons.Count; i++)
        {
            popupCloseButtons[i].OnClicked -= OnClosePanelBackgroundClicked;
        }
    }

    private void OnClosePanelBackgroundClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        CloseOperatePanel();
    }

    private void CloseOperatePanel()
    {
        if (currentOperateItemIndex < 0)
        {
            return;
        }

        currentOperateItemIndex = -1;
        DestroyCurrentPopupImmediate();
        HideCloseMask();
    }

    private void CleanupSpawnedContainers()
    {
        for (int i = 0; i < spawnedContainers.Count; i++)
        {
            spawnedContainers[i].Dispose();
        }

        spawnedContainers.Clear();
    }

    private Transform ResolvePopupLayerRoot()
    {
        if (UIManager.Instance != null && UIManager.Instance.TryGetLayerRoot(UILayerType.Popup, out Transform layerRoot))
        {
            return layerRoot;
        }

        return transform;
    }

    private void RebuildPopup(InventoryItemOperateResource resource)
    {
        DestroyCurrentPopupImmediate();

        if (resource.itemData.ItemType == ItemType.Weapon)
        {
            weaponPopupInstance = Instantiate(weaponPopupPrefab, popupLayerRoot, false);
            weaponPopupInstance.name = weaponPopupPrefab.name;
            weaponPopupInstance.transform.SetAsLastSibling();
            weaponPopupInstance.Configure(resource);
            return;
        }

        accessoryPopupInstance = Instantiate(accessoryPopupPrefab, popupLayerRoot, false);
        accessoryPopupInstance.name = accessoryPopupPrefab.name;
        accessoryPopupInstance.transform.SetAsLastSibling();
        accessoryPopupInstance.Configure(resource);
    }

    private void DestroyCurrentPopupImmediate()
    {
        if (weaponPopupInstance != null)
        {
            weaponPopupInstance.Dispose();
            Destroy(weaponPopupInstance.gameObject);
            weaponPopupInstance = null;
        }

        if (accessoryPopupInstance != null)
        {
            accessoryPopupInstance.Dispose();
            Destroy(accessoryPopupInstance.gameObject);
            accessoryPopupInstance = null;
        }
    }

    private void EnsureFullScreenCloseMask()
    {
        if (popupCloseMask != null)
        {
            return;
        }

        popupCloseMask = new GameObject("InventoryOperatePopupCloseMask", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(UIClickTarget));
        RectTransform maskRect = popupCloseMask.GetComponent<RectTransform>();
        maskRect.SetParent(popupLayerRoot, false);
        maskRect.anchorMin = Vector2.zero;
        maskRect.anchorMax = Vector2.one;
        maskRect.offsetMin = Vector2.zero;
        maskRect.offsetMax = Vector2.zero;
        maskRect.SetAsFirstSibling();

        Image maskImage = popupCloseMask.GetComponent<Image>();
        maskImage.color = new Color(0f, 0f, 0f, 0.001f);
        maskImage.raycastTarget = true;

        popupCloseButtons.Clear();
        if (closeInventoryItemOperatePanelButtons != null)
        {
            popupCloseButtons.AddRange(closeInventoryItemOperatePanelButtons);
        }

        popupCloseButtons.Add(popupCloseMask.GetComponent<UIClickTarget>());
        popupCloseMask.SetActive(false);
    }

    private void ShowCloseMask()
    {
        if (popupCloseMask == null)
        {
            return;
        }

        popupCloseMask.SetActive(true);
        popupCloseMask.transform.SetAsFirstSibling();
    }

    private void HideCloseMask()
    {
        if (popupCloseMask == null)
        {
            return;
        }

        popupCloseMask.SetActive(false);
    }

    private void ValidateConfiguration()
    {
        if (itemPrefab == null)
        {
            throw new MissingReferenceException($"{nameof(InventoryUI)} '{name}' is missing {nameof(InventoryItem)} prefab.");
        }

        if (weaponPopupPrefab == null)
        {
            throw new MissingReferenceException($"{nameof(InventoryUI)} '{name}' is missing {nameof(WeaponOperatePopup)} prefab.");
        }

        if (accessoryPopupPrefab == null)
        {
            throw new MissingReferenceException($"{nameof(InventoryUI)} '{name}' is missing {nameof(AccessoryInfoPopup)} prefab.");
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
}
