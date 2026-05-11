using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct DisplayResolutionOption : IEquatable<DisplayResolutionOption>
{
    public DisplayResolutionOption(int width, int height)
    {
        Width = Mathf.Max(1, width);
        Height = Mathf.Max(1, height);
    }

    public int Width { get; }
    public int Height { get; }
    public string Label => $"{Width} x {Height}";

    public bool Equals(DisplayResolutionOption other)
    {
        return Width == other.Width && Height == other.Height;
    }

    public override bool Equals(object obj)
    {
        return obj is DisplayResolutionOption other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Width * 397) ^ Height;
        }
    }
}

public readonly struct DisplaySettingsSnapshot : IEquatable<DisplaySettingsSnapshot>
{
    public DisplaySettingsSnapshot(int width, int height, FullScreenMode windowMode)
    {
        Width = Mathf.Max(1, width);
        Height = Mathf.Max(1, height);
        WindowMode = DisplaySettingsService.NormalizeWindowMode(windowMode);
    }

    public int Width { get; }
    public int Height { get; }
    public FullScreenMode WindowMode { get; }
    public string ResolutionLabel => $"{Width} x {Height}";
    public string WindowModeLabel => DisplaySettingsService.GetWindowModeLabel(WindowMode);

    public bool Equals(DisplaySettingsSnapshot other)
    {
        return Width == other.Width &&
               Height == other.Height &&
               WindowMode == other.WindowMode;
    }

    public override bool Equals(object obj)
    {
        return obj is DisplaySettingsSnapshot other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = Width;
            hashCode = (hashCode * 397) ^ Height;
            hashCode = (hashCode * 397) ^ (int)WindowMode;
            return hashCode;
        }
    }
}

public static class DisplaySettingsService
{
    public const int MIN_WIDTH = 1280;
    public const int MIN_HEIGHT = 720;
    public const int FALLBACK_WIDTH = 1920;
    public const int FALLBACK_HEIGHT = 1080;

    private static readonly FullScreenMode[] windowModes =
    {
        FullScreenMode.FullScreenWindow,
        FullScreenMode.ExclusiveFullScreen,
        FullScreenMode.Windowed
    };

    public static IReadOnlyList<FullScreenMode> WindowModes => windowModes;

    public static List<DisplayResolutionOption> GetAvailableResolutions()
    {
        Resolution[] unityResolutions = Screen.resolutions;
        List<DisplayResolutionOption> candidates = new List<DisplayResolutionOption>();

        if (unityResolutions != null)
        {
            for (int i = 0; i < unityResolutions.Length; i++)
            {
                Resolution resolution = unityResolutions[i];
                candidates.Add(new DisplayResolutionOption(resolution.width, resolution.height));
            }
        }

        if (candidates.Count == 0)
        {
            Resolution current = Screen.currentResolution;
            if (current.width > 0 && current.height > 0)
            {
                candidates.Add(new DisplayResolutionOption(current.width, current.height));
            }
        }

        if (candidates.Count == 0)
        {
            candidates.Add(new DisplayResolutionOption(FALLBACK_WIDTH, FALLBACK_HEIGHT));
        }

        return BuildResolutionOptions(candidates);
    }

    public static List<DisplayResolutionOption> BuildResolutionOptions(IEnumerable<DisplayResolutionOption> source)
    {
        Dictionary<string, DisplayResolutionOption> unique = new Dictionary<string, DisplayResolutionOption>(StringComparer.Ordinal);
        if (source != null)
        {
            foreach (DisplayResolutionOption option in source)
            {
                if (option.Width < MIN_WIDTH || option.Height < MIN_HEIGHT)
                {
                    continue;
                }

                string key = BuildResolutionKey(option.Width, option.Height);
                unique[key] = option;
            }
        }

        if (unique.Count == 0)
        {
            unique[BuildResolutionKey(MIN_WIDTH, MIN_HEIGHT)] = new DisplayResolutionOption(MIN_WIDTH, MIN_HEIGHT);
        }

        List<DisplayResolutionOption> result = new List<DisplayResolutionOption>(unique.Values);
        result.Sort((left, right) =>
        {
            int widthCompare = right.Width.CompareTo(left.Width);
            return widthCompare != 0 ? widthCompare : right.Height.CompareTo(left.Height);
        });

        return result;
    }

    public static DisplayResolutionOption GetDefaultResolution()
    {
        IReadOnlyList<DisplayResolutionOption> options = GetAvailableResolutions();
        return options.Count > 0 ? options[0] : new DisplayResolutionOption(FALLBACK_WIDTH, FALLBACK_HEIGHT);
    }

    public static DisplaySettingsSnapshot GetCurrentSnapshot()
    {
        int width = Screen.width > 0 ? Screen.width : GetDefaultResolution().Width;
        int height = Screen.height > 0 ? Screen.height : GetDefaultResolution().Height;
        return new DisplaySettingsSnapshot(width, height, NormalizeWindowMode(Screen.fullScreenMode));
    }

    public static void Apply(DisplaySettingsSnapshot snapshot)
    {
        Screen.SetResolution(snapshot.Width, snapshot.Height, NormalizeWindowMode(snapshot.WindowMode));
    }

    public static FullScreenMode NormalizeWindowMode(FullScreenMode mode)
    {
        for (int i = 0; i < windowModes.Length; i++)
        {
            if (windowModes[i] == mode)
            {
                return mode;
            }
        }

        return FullScreenMode.FullScreenWindow;
    }

    public static string GetWindowModeLabel(FullScreenMode mode)
    {
        switch (NormalizeWindowMode(mode))
        {
            case FullScreenMode.ExclusiveFullScreen:
                return "Exclusive Fullscreen";
            case FullScreenMode.Windowed:
                return "Windowed";
            default:
                return "Fullscreen Window";
        }
    }

    public static string Format(DisplaySettingsSnapshot snapshot)
    {
        return $"{snapshot.ResolutionLabel} / {snapshot.WindowModeLabel}";
    }

    private static string BuildResolutionKey(int width, int height)
    {
        return $"{width}x{height}";
    }
}
