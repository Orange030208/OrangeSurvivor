public sealed class ShopPageState
{
    public bool IsPropertiesSidebarVisible { get; private set; } = true;
    public bool IsInventorySidebarVisible { get; private set; } = true;

    public void TogglePropertiesSidebar()
    {
        IsPropertiesSidebarVisible = !IsPropertiesSidebarVisible;
    }

    public void ToggleInventorySidebar()
    {
        IsInventorySidebarVisible = !IsInventorySidebarVisible;
    }
}
