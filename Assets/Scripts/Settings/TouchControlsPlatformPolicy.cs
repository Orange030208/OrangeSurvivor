using UnityEngine;

public static class TouchControlsPlatformPolicy
{
    public static bool IsTouchControlsEnabled(
        PlatformSettingsProfileSO[] platformProfiles,
        RuntimePlatform platform)
    {
        PlatformSettingsProfileSO profile = PlatformSettingsProfileSO.SelectProfile(platformProfiles, platform);
        return profile != null && profile.IsEnabled(SettingsFeature.TouchControls);
    }
}
