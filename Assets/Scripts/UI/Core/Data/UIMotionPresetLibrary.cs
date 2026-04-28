using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = ScriptableObjectMenuPaths.UI_MOTION_PRESET_LIBRARY, fileName = "UIMotionPresetLibrary")]
public sealed class UIMotionPresetLibrary : ScriptableObject
{
    [SerializeField] private List<UIMotionPresetEntry> entries = new();

    public IReadOnlyList<UIMotionPresetEntry> Entries => entries;

    public bool TryGetPreset(string option, out UIMotionPreset preset)
    {
        preset = null;
        if (!TryGetEntry(option, out UIMotionPresetEntry entry))
        {
            return false;
        }

        preset = entry.Preset;
        return preset != null;
    }

    public bool TryGetPreset<TPreset>(string option, out TPreset preset)
        where TPreset : UIMotionPreset
    {
        preset = null;
        if (!TryGetPreset(option, out UIMotionPreset motionPreset))
        {
            return false;
        }

        preset = motionPreset as TPreset;
        return preset != null;
    }

    public bool TryGetEntry(string option, out UIMotionPresetEntry result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(option) || entries == null)
        {
            return false;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            UIMotionPresetEntry entry = entries[i];
            if (entry == null || entry.Preset == null)
            {
                continue;
            }

            if (!string.Equals(entry.Option, option, StringComparison.Ordinal))
            {
                continue;
            }

            result = entry;
            return true;
        }

        return false;
    }

    public bool TryGetOption(UIMotionPreset preset, out string option)
    {
        option = null;
        if (preset == null || entries == null)
        {
            return false;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            UIMotionPresetEntry entry = entries[i];
            if (entry == null || entry.Preset != preset)
            {
                continue;
            }

            option = entry.Option;
            return true;
        }

        return false;
    }
}
