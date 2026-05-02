using AXR.Framework.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class InventoryPopupHostView
{
    private readonly string ownerName;
    private readonly WeaponOperatePopup weaponPopupPrefab;
    private readonly AccessoryInfoPopup accessoryPopupPrefab;
    private readonly Transform popupLayerRoot;
    private readonly List<UIClickTarget> popupCloseButtons = new();

    private bool closeHandlersBound;
    private GameObject popupCloseMask;
    private WeaponOperatePopup weaponPopupInstance;
    private AccessoryInfoPopup accessoryPopupInstance;

    public event System.Action CloseRequested;
    public event System.Action<string> SellRequested;
    public event System.Action<string> MergeRequested;

    public InventoryPopupHostView(
        string ownerName,
        WeaponOperatePopup weaponPopupPrefab,
        AccessoryInfoPopup accessoryPopupPrefab,
        Transform popupLayerRoot,
        UIClickTarget[] closeButtons)
    {
        this.ownerName = string.IsNullOrWhiteSpace(ownerName) ? nameof(InventoryPopupHostView) : ownerName;
        this.weaponPopupPrefab = weaponPopupPrefab ?? throw new MissingReferenceException($"{nameof(InventoryUI)} '{this.ownerName}' is missing {nameof(WeaponOperatePopup)} prefab.");
        this.accessoryPopupPrefab = accessoryPopupPrefab ?? throw new MissingReferenceException($"{nameof(InventoryUI)} '{this.ownerName}' is missing {nameof(AccessoryInfoPopup)} prefab.");
        this.popupLayerRoot = popupLayerRoot ?? throw new MissingReferenceException($"{nameof(InventoryUI)} '{this.ownerName}' failed to resolve popup layer root.");

        EnsureFullScreenCloseMask(closeButtons);
    }

    public string CurrentEntryId { get; private set; }

    public bool HasOpenPopup => !string.IsNullOrEmpty(CurrentEntryId);

    public void BindCloseHandlers()
    {
        if (closeHandlersBound)
        {
            return;
        }

        for (int i = 0; i < popupCloseButtons.Count; i++)
        {
            popupCloseButtons[i].OnClicked += OnClosePanelBackgroundClicked;
        }

        closeHandlersBound = true;
    }

    public void UnbindCloseHandlers()
    {
        if (!closeHandlersBound)
        {
            return;
        }

        for (int i = 0; i < popupCloseButtons.Count; i++)
        {
            popupCloseButtons[i].OnClicked -= OnClosePanelBackgroundClicked;
        }

        closeHandlersBound = false;
    }

    public void Show(InventoryItemOperateResource resource)
    {
        if (resource.itemData == null)
        {
            return;
        }

        CurrentEntryId = resource.entryId;
        ShowCloseMask();
        RebuildPopup(resource);
    }

    public bool IsShowingItem(string entryId)
    {
        return CurrentEntryId == entryId;
    }

    public void CloseCurrent()
    {
        CurrentEntryId = null;
        DestroyCurrentPopupImmediate();
        HideCloseMask();
    }

    private void RebuildPopup(InventoryItemOperateResource resource)
    {
        DestroyCurrentPopupImmediate();

        if (resource.itemData.ItemType == ItemType.Weapon)
        {
            weaponPopupInstance = UnityEngine.Object.Instantiate(weaponPopupPrefab, popupLayerRoot, false);
            weaponPopupInstance.name = weaponPopupPrefab.name;
            weaponPopupInstance.transform.SetAsLastSibling();
            weaponPopupInstance.Configure(resource);
            weaponPopupInstance.SellRequested += OnSellRequested;
            weaponPopupInstance.MergeRequested += OnMergeRequested;
            return;
        }

        accessoryPopupInstance = UnityEngine.Object.Instantiate(accessoryPopupPrefab, popupLayerRoot, false);
        accessoryPopupInstance.name = accessoryPopupPrefab.name;
        accessoryPopupInstance.transform.SetAsLastSibling();
        accessoryPopupInstance.Configure(resource);
    }

    private void DestroyCurrentPopupImmediate()
    {
        if (weaponPopupInstance != null)
        {
            weaponPopupInstance.SellRequested -= OnSellRequested;
            weaponPopupInstance.MergeRequested -= OnMergeRequested;
            weaponPopupInstance.Dispose();
            UnityEngine.Object.Destroy(weaponPopupInstance.gameObject);
            weaponPopupInstance = null;
        }

        if (accessoryPopupInstance != null)
        {
            accessoryPopupInstance.Dispose();
            UnityEngine.Object.Destroy(accessoryPopupInstance.gameObject);
            accessoryPopupInstance = null;
        }
    }

    private void EnsureFullScreenCloseMask(UIClickTarget[] closeButtons)
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
        if (closeButtons != null)
        {
            popupCloseButtons.AddRange(closeButtons);
        }

        popupCloseButtons.Add(popupCloseMask.GetComponent<UIClickTarget>());
        popupCloseMask.SetActive(false);
    }

    private void OnClosePanelBackgroundClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        CloseRequested?.Invoke();
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

    private void OnSellRequested(string entryId)
    {
        SellRequested?.Invoke(entryId);
    }

    private void OnMergeRequested(string entryId)
    {
        MergeRequested?.Invoke(entryId);
    }
}
