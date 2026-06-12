using UnityEngine;

public static class GameSettingsService
{
    public const string DEFAULT_LANGUAGE_CODE = "zh-CN";
    public const string ENGLISH_LANGUAGE_CODE = "en-US";

    private const string MASTER_VOLUME_KEY = "Settings.MasterVolume";
    private const string SFX_VOLUME_KEY = "Settings.SfxVolume";
    private const string MUSIC_VOLUME_KEY = "Settings.MusicVolume";
    private const string RESOLUTION_WIDTH_KEY = "Settings.ResolutionWidth";
    private const string RESOLUTION_HEIGHT_KEY = "Settings.ResolutionHeight";
    private const string WINDOW_MODE_KEY = "Settings.WindowMode";
    private const string LANGUAGE_CODE_KEY = "Settings.LanguageCode";
    private const string INPUT_REBINDS_JSON_KEY = "Settings.InputRebindsJson";

    public static GameSettingsState Current { get; private set; } = GameSettingsState.Default();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplySavedSettingsOnSceneLoad()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Load();
        Apply(Current, applyDisplay: true, applyInput: true);
    }

    public static GameSettingsState Load()
    {
        GameSettingsState defaults = GameSettingsState.Default();
        Current = new GameSettingsState
        {
            MasterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, defaults.MasterVolume),
            SfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, defaults.SfxVolume),
            MusicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, defaults.MusicVolume),
            ResolutionWidth = PlayerPrefs.GetInt(RESOLUTION_WIDTH_KEY, defaults.ResolutionWidth),
            ResolutionHeight = PlayerPrefs.GetInt(RESOLUTION_HEIGHT_KEY, defaults.ResolutionHeight),
            WindowMode = (FullScreenMode)PlayerPrefs.GetInt(WINDOW_MODE_KEY, (int)defaults.WindowMode),
            LanguageCode = PlayerPrefs.GetString(LANGUAGE_CODE_KEY, defaults.LanguageCode),
            InputRebindsJson = PlayerPrefs.GetString(INPUT_REBINDS_JSON_KEY, defaults.InputRebindsJson)
        };

        Current.Sanitize();
        return Current.Clone();
    }

    public static void Save(GameSettingsState state)
    {
        Current = state?.Clone() ?? GameSettingsState.Default();
        Current.Sanitize();

        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, Current.MasterVolume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, Current.SfxVolume);
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, Current.MusicVolume);
        PlayerPrefs.SetInt(RESOLUTION_WIDTH_KEY, Current.ResolutionWidth);
        PlayerPrefs.SetInt(RESOLUTION_HEIGHT_KEY, Current.ResolutionHeight);
        PlayerPrefs.SetInt(WINDOW_MODE_KEY, (int)Current.WindowMode);
        PlayerPrefs.SetString(LANGUAGE_CODE_KEY, Current.LanguageCode);
        PlayerPrefs.SetString(INPUT_REBINDS_JSON_KEY, Current.InputRebindsJson ?? string.Empty);
        PlayerPrefs.Save();
    }

    public static void SaveInputRebinds(string rebindsJson)
    {
        GameSettingsState state = Load();
        state.InputRebindsJson = rebindsJson ?? string.Empty;
        Save(state);
        GameInput input = GameInput.Instance;
        input?.LoadBindingOverrides(state.InputRebindsJson);
        input?.SaveBindingOverridesToStore();
        ApplyInput(state.InputRebindsJson);
    }

    public static void Apply(GameSettingsState state, bool applyDisplay, bool applyInput)
    {
        if (state == null)
        {
            return;
        }

        state.Sanitize();
        ApplyAudio(state);

        if (applyDisplay)
        {
            DisplaySettingsService.Apply(state.ToDisplaySnapshot());
        }

        if (applyInput)
        {
            ApplyInput(state.InputRebindsJson);
        }
    }

    public static void ApplyAudio(GameSettingsState state)
    {
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null || state == null)
        {
            return;
        }

        audioManager.SetMasterVolume(state.MasterVolume);
        audioManager.SetSfxVolume(state.SfxVolume);
        audioManager.SetMusicVolume(state.MusicVolume);
    }

    public static string NormalizeLanguageCode(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return DEFAULT_LANGUAGE_CODE;
        }

        string normalized = languageCode.Trim();
        return normalized == ENGLISH_LANGUAGE_CODE ? ENGLISH_LANGUAGE_CODE : DEFAULT_LANGUAGE_CODE;
    }

    private static void ApplyInput(string rebindsJson)
    {
        if (!Application.isPlaying)
        {
            return;
        }

        GameInput input = GameInput.Instance;
        if (input != null)
        {
            if (!input.LoadBindingOverridesFromStore())
            {
                input.LoadBindingOverrides(rebindsJson);
            }
        }
    }
}
