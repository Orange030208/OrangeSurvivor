public sealed class ShopPropertiesPopupContext
{
    public ShopPropertiesPopupContext(PropertiesManager propertiesManager)
    {
        PropertiesManager = propertiesManager;
    }

    public PropertiesManager PropertiesManager { get; }
}
