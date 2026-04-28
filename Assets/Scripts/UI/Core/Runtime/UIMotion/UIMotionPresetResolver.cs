using UnityEngine;

public static class UIMotionPresetResolver
{
    private const string LIBRARY_RESOURCE_PATH = "Data/UI/Motion/UIMotionPresetLibrary";

    private static UIMotionPresetLibrary cachedLibrary;

    public static bool TryGetPreset(string option, out UIMotionPreset preset)
    {
        preset = null;
        UIMotionPresetLibrary library = GetLibrary();
        return library != null && library.TryGetPreset(option, out preset);
    }

    public static bool TryGetPreset<TPreset>(string option, out TPreset preset)
        where TPreset : UIMotionPreset
    {
        preset = null;
        UIMotionPresetLibrary library = GetLibrary();
        return library != null && library.TryGetPreset(option, out preset);
    }

    public static bool TryGetEntry(string option, out UIMotionPresetEntry entry)
    {
        entry = null;
        UIMotionPresetLibrary library = GetLibrary();
        return library != null && library.TryGetEntry(option, out entry);
    }

    public static bool TryGetOption(UIMotionPreset preset, out string option)
    {
        option = null;
        UIMotionPresetLibrary library = GetLibrary();
        return library != null && library.TryGetOption(preset, out option);
    }

    public static void ClearCache()
    {
        cachedLibrary = null;
    }

    private static UIMotionPresetLibrary GetLibrary()
    {
        if (cachedLibrary != null)
        {
            return cachedLibrary;
        }

        cachedLibrary = Resources.Load<UIMotionPresetLibrary>(LIBRARY_RESOURCE_PATH);
        return cachedLibrary;
    }
}
