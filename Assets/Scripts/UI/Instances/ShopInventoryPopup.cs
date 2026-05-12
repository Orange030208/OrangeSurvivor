using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

public class ShopInventoryPopup : PopupBase
{
    [SerializeField] private InventoryUI inventoryUI;

    private ShopInventoryPopupContext popupContext;

    protected override void Awake()
    {
        base.Awake();
        ResolveViewParts();
        ValidateConfiguration();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        ShopInventoryPopupContext inventoryContext = context.GetPayload<ShopInventoryPopupContext>();
        if (inventoryContext == null)
        {
            ShopPageContext shopPageContext = context.GetPayload<ShopPageContext>();
            if (shopPageContext == null)
            {
                throw new ArgumentException($"{nameof(ShopInventoryPopup)} requires {nameof(ShopInventoryPopupContext)} payload.");
            }

            inventoryContext = new ShopInventoryPopupContext(shopPageContext.InventoryOperateManager, OwnerUIManager);
        }

        Configure(inventoryContext);
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        inventoryUI?.ReleaseSession();
        popupContext = null;
    }

    private void Configure(ShopInventoryPopupContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        popupContext = context;
        ResolveViewParts();
        ValidateConfiguration();
        inventoryUI.ConfigureSession(popupContext.InventoryOperateManager, popupContext.UIManager);
    }

    private void ResolveViewParts()
    {
        if (inventoryUI == null)
        {
            inventoryUI = GetComponentInChildren<InventoryUI>(true);
        }
    }

    private void ValidateConfiguration()
    {
        if (inventoryUI == null)
        {
            throw new MissingReferenceException($"{nameof(ShopInventoryPopup)} '{name}' is missing inventory UI.");
        }
    }
}
