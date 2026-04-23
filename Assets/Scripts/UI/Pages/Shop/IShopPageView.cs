using System;

public interface IShopPageView
{
    event Action RerollRequested;
    event Action ContinueRequested;
    event Action PropertiesToggleRequested;
    event Action InventoryToggleRequested;
    event Action<int> ItemBuyRequested;
    event Action<int> ItemLockToggleRequested;

    void PrepareForOpen(ShopPageContext context);
    void ResetAfterClose();
    void RenderShopItems(ShopItemData[] items);
    void UpdateRerollState(int rerollCost, bool canReroll);
    void UpdateCurrencyAmount(int amount);
    void ShowPurchaseSuccess(ShopPurchaseSuccessEvent eventData);
    void ShowPurchaseFailure(string message);
    void SetPropertiesSidebarVisible(bool visible);
    void SetInventorySidebarVisible(bool visible);
}
