public static class AudioConstants
{
    public const string AUDIO_MENU_ROOT = ScriptableObjectMenuPaths.SYSTEMS_ROOT + "Audio/";
    public const string AUDIO_CATALOG_MENU_PATH = AUDIO_MENU_ROOT + "Cue Catalog";
    public const string AUDIO_RUNTIME_SETTINGS_MENU_PATH = ScriptableObjectMenuPaths.AUDIO_RUNTIME_SETTINGS;
    public const string AUDIO_BUS_SETTINGS_MENU_PATH = ScriptableObjectMenuPaths.AUDIO_BUS_SETTINGS;
    public const string DEFAULT_CUE_ID = "Audio_NewCue";
    public const string CUE_ID_PREFIX = "Audio_";
    public const string DEFAULT_UI_CLICK_CUE_ID = "Audio_UI_Click";
    public const float MIN_VOLUME = 0f;
    public const float MAX_VOLUME = 1f;
    public const float DEFAULT_VOLUME = 1f;
    public const float DEFAULT_PITCH = 1f;
    public const float MIN_PITCH = -3f;
    public const float MAX_PITCH = 3f;
    public const float DEFAULT_MUSIC_FADE_DURATION = 0.25f;
    public const float MIN_FADE_DURATION = 0f;
    public const string DEFAULT_SFX_GROUP_ID = "sfx.default";
    public const string UI_SFX_GROUP_ID = "sfx.ui";
    public const string COMBAT_SFX_GROUP_ID = "sfx.combat";
    public const string PICKUP_SFX_GROUP_ID = "sfx.pickup";
    public const string AMBIENT_SFX_GROUP_ID = "sfx.ambient";
    public const int DEFAULT_SFX_POOL_SIZE = 16;
    public const int DEFAULT_SFX_MAX_CONCURRENT = 12;
    public const int DEFAULT_CUE_MAX_CONCURRENT = 4;
    public const int MIN_POOL_SIZE = 1;
    public const int MAX_POOL_SIZE = 64;
    public const int MIN_CONCURRENT_COUNT = 1;
    public const int MAX_CONCURRENT_COUNT = 64;
    public const int DEFAULT_AUDIO_PRIORITY = 128;
    public const float DEFAULT_2D_AUDIBLE_DISTANCE = 14f;
    public const float MIN_2D_AUDIBLE_DISTANCE = 0.01f;
}
