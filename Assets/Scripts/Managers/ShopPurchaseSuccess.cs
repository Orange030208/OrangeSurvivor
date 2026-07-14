public readonly struct ShopPurchaseSuccess
{
    public ShopOfferSnapshot Offer { get; }

    public ShopPurchaseSuccess(ShopOfferSnapshot offer)
    {
        Offer = offer;
    }
}
