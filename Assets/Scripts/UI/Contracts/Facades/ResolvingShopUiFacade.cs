using System;
using UnityEngine;

public sealed class ResolvingShopUiFacade : IShopUiFacade
{
    private readonly CurrencyWallet currencyWallet;
    private IShopUiFacade directFacade;
    private bool active;
    private bool directFacadeBound;
    private bool missingManagerWarningLogged;

    public ResolvingShopUiFacade(CurrencyWallet currencyWallet)
    {
        this.currencyWallet = currencyWallet;
    }

    public event Action<ShopSnapshot> SnapshotChanged;
    public event Action<ShopPurchaseSuccess> PurchaseSucceeded;
    public event Action<ShopPurchaseFailure> PurchaseFailed;
    public event Action<int> CurrencyChanged;

    public void Activate()
    {
        if (active)
        {
            return;
        }

        active = true;
        EnsureDirectFacadeActive();
    }

    public void Deactivate()
    {
        if (!active)
        {
            return;
        }

        if (directFacadeBound)
        {
            directFacade.SnapshotChanged -= OnSnapshotChanged;
            directFacade.PurchaseSucceeded -= OnPurchaseSucceeded;
            directFacade.PurchaseFailed -= OnPurchaseFailed;
            directFacade.CurrencyChanged -= OnCurrencyChanged;
            directFacade.Deactivate();
            directFacadeBound = false;
        }

        active = false;
    }

    public void RequestSnapshot()
    {
        if (TryGetDirectFacade(out IShopUiFacade facade))
        {
            facade.RequestSnapshot();
        }
    }

    public void RequestReroll()
    {
        if (TryGetDirectFacade(out IShopUiFacade facade))
        {
            facade.RequestReroll();
        }
    }

    public void RequestContinue()
    {
        GameEventBus.Publish<ShopContinueClickedEvent>();
    }

    public void RequestBuyItem(int itemIndex)
    {
        if (TryGetDirectFacade(out IShopUiFacade facade))
        {
            facade.RequestBuyItem(itemIndex);
        }
    }

    public void RequestToggleLock(int itemIndex)
    {
        if (TryGetDirectFacade(out IShopUiFacade facade))
        {
            facade.RequestToggleLock(itemIndex);
        }
    }

    public void Dispose()
    {
        Deactivate();
        directFacade?.Dispose();
        directFacade = null;
    }

    private void OnSnapshotChanged(ShopSnapshot snapshot)
    {
        SnapshotChanged?.Invoke(snapshot);
    }

    private void OnPurchaseSucceeded(ShopPurchaseSuccess result)
    {
        PurchaseSucceeded?.Invoke(result);
    }

    private void OnPurchaseFailed(ShopPurchaseFailure failure)
    {
        PurchaseFailed?.Invoke(failure);
    }

    private void OnCurrencyChanged(int currentAmount)
    {
        CurrencyChanged?.Invoke(currentAmount);
    }

    private bool TryGetDirectFacade(out IShopUiFacade facade)
    {
        EnsureDirectFacadeActive();
        facade = directFacade;

        if (facade != null)
        {
            return true;
        }

        if (!missingManagerWarningLogged)
        {
            Debug.LogWarning($"{nameof(ResolvingShopUiFacade)} failed to resolve {nameof(ShopManager)}. Shop UI commands will be ignored.");
            missingManagerWarningLogged = true;
        }

        return false;
    }

    private void EnsureDirectFacadeActive()
    {
        if (!active || directFacadeBound)
        {
            return;
        }

        if (directFacade == null)
        {
            ShopManager shopManager = UnityEngine.Object.FindFirstObjectByType<ShopManager>();
            if (shopManager == null)
            {
                return;
            }

            directFacade = new ManagerShopUiFacade(shopManager, currencyWallet);
        }

        directFacade.SnapshotChanged += OnSnapshotChanged;
        directFacade.PurchaseSucceeded += OnPurchaseSucceeded;
        directFacade.PurchaseFailed += OnPurchaseFailed;
        directFacade.CurrencyChanged += OnCurrencyChanged;
        directFacade.Activate();
        directFacadeBound = true;
    }
}
