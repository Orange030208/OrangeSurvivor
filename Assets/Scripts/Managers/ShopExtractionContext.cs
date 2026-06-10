public readonly struct ShopExtractionContext
{
    public ShopExtractionContext(AccessoryManager accessoryManager)
    {
        AccessoryManager = accessoryManager;
    }

    public AccessoryManager AccessoryManager { get; }
}
