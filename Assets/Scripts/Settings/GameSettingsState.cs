using System;
using UnityEngine;

[Serializable]
public sealed class GameSettingsState
{
    public float MasterVolume = AudioConstants.DEFAULT_VOLUME;
    public float SfxVolume = AudioConstants.DEFAULT_VOLUME;
    public float MusicVolume = AudioConstants.DEFAULT_VOLUME;
    public int ResolutionWidth;
    public int ResolutionHeight;
    public FullScreenMode WindowMode = FullScreenMode.FullScreenWindow;
    public string LanguageCode = GameSettingsService.DEFAULT_LANGUAGE_CODE;
    public string InputRebindsJson = string.Empty;

    public GameSettingsState()
    {
        ResolutionWidth = DisplaySettingsService.FALLBACK_WIDTH;
        ResolutionHeight = DisplaySettingsService.FALLBACK_HEIGHT;
    }

    public GameSettingsState Clone()
    {
        return new GameSettingsState
        {
            MasterVolume = MasterVolume,
            SfxVolume = SfxVolume,
            MusicVolume = MusicVolume,
            ResolutionWidth = ResolutionWidth,
            ResolutionHeight = ResolutionHeight,
            WindowMode = WindowMode,
            LanguageCode = LanguageCode,
            InputRebindsJson = InputRebindsJson
        };
    }

    public void Sanitize()
    {
        MasterVolume = Mathf.Clamp01(MasterVolume);
        SfxVolume = Mathf.Clamp01(SfxVolume);
        MusicVolume = Mathf.Clamp01(MusicVolume);
        WindowMode = DisplaySettingsService.NormalizeWindowMode(WindowMode);
        LanguageCode = GameSettingsService.NormalizeLanguageCode(LanguageCode);

        if (ResolutionWidth < DisplaySettingsService.MIN_WIDTH || ResolutionHeight < DisplaySettingsService.MIN_HEIGHT)
        {
            DisplayResolutionOption defaultResolution = DisplaySettingsService.GetDefaultResolution();
            ResolutionWidth = defaultResolution.Width;
            ResolutionHeight = defaultResolution.Height;
        }

        InputRebindsJson ??= string.Empty;
    }

    public DisplaySettingsSnapshot ToDisplaySnapshot()
    {
        return new DisplaySettingsSnapshot(ResolutionWidth, ResolutionHeight, WindowMode);
    }

    public void SetDisplaySnapshot(DisplaySettingsSnapshot snapshot)
    {
        ResolutionWidth = snapshot.Width;
        ResolutionHeight = snapshot.Height;
        WindowMode = snapshot.WindowMode;
    }

    public static GameSettingsState Default()
    {
        return new GameSettingsState();
    }
}
