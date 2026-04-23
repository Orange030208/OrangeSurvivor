using System;

public sealed class SidebarRegionGroup
{
    private readonly ISidebarRegion[] regions;

    public SidebarRegionGroup(params ISidebarRegion[] regions)
    {
        this.regions = regions ?? Array.Empty<ISidebarRegion>();
    }

    public void SetVisible(bool visible)
    {
        for (int i = 0; i < regions.Length; i++)
        {
            regions[i]?.SetVisible(visible);
        }
    }

    public void RefreshDefaults()
    {
        for (int i = 0; i < regions.Length; i++)
        {
            regions[i]?.RefreshDefaults();
        }
    }

    public void Kill()
    {
        for (int i = 0; i < regions.Length; i++)
        {
            regions[i]?.Kill();
        }
    }
}
