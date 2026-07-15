public readonly struct ShopPurchaseSuccess
{
    public ShopOfferSnapshot Offer { get; }
    public int Price { get; }

    public ShopPurchaseSuccess(ShopOfferSnapshot offer, int price)
    {
        Offer = offer;
        Price = price;
    }
}
