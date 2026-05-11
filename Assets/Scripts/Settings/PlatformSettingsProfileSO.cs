using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Platform Settings Profile",
    menuName = ScriptableObjectMenuPaths.SYSTEMS_ROOT + "Settings/Platform Settings Profile")]
public sealed class PlatformSettingsProfileSO : ScriptableObject
{
    [SerializeField] private string profileId = "PC";
    [SerializeField] private RuntimePlatform[] platforms = Array.Empty<RuntimePlatform>();
    [SerializeField] private SettingsFeature[] enabledFeatures =
    {
        SettingsFeature.Audio,
        SettingsFeature.DisplayResolution,
        SettingsFeature.WindowMode,
        SettingsFeature.Language,
        SettingsFeature.KeyboardRebind,
        SettingsFeature.GamepadRebind
    };
    [SerializeField] private FullScreenMode[] windowModes =
    {
        FullScreenMode.FullScreenWindow,
        FullScreenMode.ExclusiveFullScreen,
        FullScreenMode.Windowed
    };
    [SerializeField] private string[] languageCodes =
    {
        GameSettingsService.DEFAULT_LANGUAGE_CODE,
        GameSettingsService.ENGLISH_LANGUAGE_CODE
    };
    [SerializeField] private bool requireDisplayConfirmation = true;
    [SerializeField] private bool defaultProfile;

    public string ProfileId => string.IsNullOrWhiteSpace(profileId) ? name : profileId;
    public IReadOnlyList<RuntimePlatform> Platforms => platforms ?? Array.Empty<RuntimePlatform>();
    public IReadOnlyList<FullScreenMode> WindowModes => GetEffectiveWindowModes();
    public IReadOnlyList<string> LanguageCodes => GetEffectiveLanguageCodes();
    public bool RequireDisplayConfirmation => requireDisplayConfirmation;
    public bool DefaultProfile => defaultProfile;

    public bool Supports(RuntimePlatform platform)
    {
        if (platforms == null)
        {
            return false;
        }

        for (int i = 0; i < platforms.Length; i++)
        {
            if (platforms[i] == platform)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsEnabled(SettingsFeature feature)
    {
        if (enabledFeatures == null)
        {
            return false;
        }

        for (int i = 0; i < enabledFeatures.Length; i++)
        {
            if (enabledFeatures[i] == feature)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsAnyEnabled(params SettingsFeature[] features)
    {
        if (features == null)
        {
            return false;
        }

        for (int i = 0; i < features.Length; i++)
        {
            if (IsEnabled(features[i]))
            {
                return true;
            }
        }

        return false;
    }

    public FullScreenMode GetWindowModeAt(int index)
    {
        FullScreenMode[] modes = GetEffectiveWindowModes();
        return modes[Mathf.Clamp(index, 0, modes.Length - 1)];
    }

    public int GetWindowModeCount()
    {
        return GetEffectiveWindowModes().Length;
    }

    public int IndexOfWindowMode(FullScreenMode mode)
    {
        FullScreenMode normalized = DisplaySettingsService.NormalizeWindowMode(mode);
        FullScreenMode[] modes = GetEffectiveWindowModes();
        for (int i = 0; i < modes.Length; i++)
        {
            if (DisplaySettingsService.NormalizeWindowMode(modes[i]) == normalized)
            {
                return i;
            }
        }

        return 0;
    }

    public string GetLanguageAt(int index)
    {
        string[] languages = GetEffectiveLanguageCodes();
        return languages[Mathf.Clamp(index, 0, languages.Length - 1)];
    }

    public int GetLanguageCount()
    {
        return GetEffectiveLanguageCodes().Length;
    }

    public int IndexOfLanguage(string languageCode)
    {
        string normalized = GameSettingsService.NormalizeLanguageCode(languageCode);
        string[] languages = GetEffectiveLanguageCodes();
        for (int i = 0; i < languages.Length; i++)
        {
            if (GameSettingsService.NormalizeLanguageCode(languages[i]) == normalized)
            {
                return i;
            }
        }

        return 0;
    }

    public static PlatformSettingsProfileSO SelectProfile(PlatformSettingsProfileSO[] profiles, RuntimePlatform platform)
    {
        if (profiles == null || profiles.Length == 0)
        {
            return null;
        }

        PlatformSettingsProfileSO defaultCandidate = null;
        for (int i = 0; i < profiles.Length; i++)
        {
            PlatformSettingsProfileSO profile = profiles[i];
            if (profile == null)
            {
                continue;
            }

            if (profile.Supports(platform))
            {
                return profile;
            }

            if (profile.DefaultProfile && defaultCandidate == null)
            {
                defaultCandidate = profile;
            }
        }

        return defaultCandidate != null ? defaultCandidate : profiles[0];
    }

    private FullScreenMode[] GetEffectiveWindowModes()
    {
        return windowModes != null && windowModes.Length > 0
            ? windowModes
            : new[] { FullScreenMode.FullScreenWindow };
    }

    private string[] GetEffectiveLanguageCodes()
    {
        return languageCodes != null && languageCodes.Length > 0
            ? languageCodes
            : new[] { GameSettingsService.DEFAULT_LANGUAGE_CODE };
    }
}
