using System;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

public sealed class InventoryPopupHostView
{
    private const string POPUP_GROUP_ID = "inventory.operate";

    private readonly string ownerName;

    private int popupVersion;
    private ViewHandle currentPopupHandle;

    public event System.Action CloseRequested;
    public event System.Action<string> SellRequested;
    public event System.Action<string> MergeRequested;

    public InventoryPopupHostView(string ownerName)
    {
        this.ownerName = string.IsNullOrWhiteSpace(ownerName) ? nameof(InventoryPopupHostView) : ownerName;
    }

    public string CurrentEntryId { get; private set; }

    public bool HasOpenPopup => !string.IsNullOrEmpty(CurrentEntryId);

    public void Show(InventoryItemOperateResource resource)
    {
        if (resource.itemData == null)
        {
            return;
        }

        int version = ++popupVersion;
        CurrentEntryId = resource.entryId;
        ShowAsync(resource, version).Forget();
    }

    public bool IsShowingItem(string entryId)
    {
        return CurrentEntryId == entryId;
    }

    public void CloseCurrent()
    {
        popupVersion++;
        CurrentEntryId = null;
        CloseCurrentPopupHandleAsync(CloseReason.Normal).Forget();
    }

    private async UniTaskVoid ShowAsync(InventoryItemOperateResource resource, int version)
    {
        try
        {
            await CloseCurrentPopupHandleAsync(CloseReason.Replace);
            if (version != popupVersion)
            {
                return;
            }

            PopupOptions options = new PopupOptions(
                closeOnOutsideClick: true,
                groupId: POPUP_GROUP_ID,
                replaceSameGroup: true,
                trackInStack: true,
                preferredAnchor: FloatingViewAnchor.Center);

            if (resource.itemData.ItemType == ItemType.Weapon)
            {
                UIManager uiManager = ResolveUIManager();
                ViewHandle<WeaponOperatePopup> handle = await uiManager.ShowPopupAsync<WeaponOperatePopup>(resource, options);
                if (version != popupVersion)
                {
                    await handle.CloseAsync(CloseReason.Cancel);
                    return;
                }

                currentPopupHandle = handle.AsUntyped();
                handle.View.SellRequested += OnSellRequested;
                handle.View.MergeRequested += OnMergeRequested;
                ObservePopupClosedAsync(currentPopupHandle, version, resource.entryId).Forget();
                return;
            }

            UIManager uiManager = ResolveUIManager();
            ViewHandle<AccessoryInfoPopup> accessoryHandle = await uiManager.ShowPopupAsync<AccessoryInfoPopup>(resource, options);
            if (version != popupVersion)
            {
                await accessoryHandle.CloseAsync(CloseReason.Cancel);
                return;
            }

            currentPopupHandle = accessoryHandle.AsUntyped();
            ObservePopupClosedAsync(currentPopupHandle, version, resource.entryId).Forget();
        }
        catch (Exception exception)
        {
            if (version == popupVersion)
            {
                CurrentEntryId = null;
            }

            Debug.LogException(exception);
        }
    }

    private async UniTaskVoid ObservePopupClosedAsync(ViewHandle handle, int version, string entryId)
    {
        try
        {
            await handle.ClosedTask;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }

        if (version != popupVersion || !string.Equals(CurrentEntryId, entryId, StringComparison.Ordinal))
        {
            return;
        }

        CurrentEntryId = null;
        currentPopupHandle = default;
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        CloseRequested?.Invoke();
    }

    private async UniTask CloseCurrentPopupHandleAsync(CloseReason reason)
    {
        ViewHandle handle = currentPopupHandle;
        currentPopupHandle = default;
        if (!handle.IsValid)
        {
            return;
        }

        await handle.CloseAsync(reason);
    }

    private UIManager ResolveUIManager()
    {
        if (UIManager.Instance != null)
        {
            return UIManager.Instance;
        }

        throw new MissingReferenceException($"{nameof(InventoryUI)} '{ownerName}' requires an active {nameof(UIManager)} before inventory operate popups can be opened.");
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
