public readonly struct ShopPurchaseFailure
{
    public string Message { get; }

    public ShopPurchaseFailure(string message)
    {
        Message = message;
    }
}
