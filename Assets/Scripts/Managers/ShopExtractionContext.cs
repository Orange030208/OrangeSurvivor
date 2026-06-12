public readonly struct ShopExtractionContext
{
    public ShopExtractionContext(AccessoryManager accessoryManager, float luck)
    {
        AccessoryManager = accessoryManager;
        Luck = luck;
    }

    public AccessoryManager AccessoryManager { get; }
    public float Luck { get; }
}
