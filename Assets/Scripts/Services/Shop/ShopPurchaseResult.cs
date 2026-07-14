/// <summary>
/// 商品购买执行结果。扣钱和货架状态变化由 ShopManager 统一处理。
/// </summary>
public readonly struct ShopPurchaseResult
{
    private ShopPurchaseResult(bool succeeded, string failureMessage)
    {
        Succeeded = succeeded;
        FailureMessage = failureMessage ?? string.Empty;
    }

    public bool Succeeded { get; }
    public string FailureMessage { get; }

    public static ShopPurchaseResult Success()
    {
        return new ShopPurchaseResult(true, string.Empty);
    }

    public static ShopPurchaseResult Failure(string message)
    {
        return new ShopPurchaseResult(false, message);
    }
}
