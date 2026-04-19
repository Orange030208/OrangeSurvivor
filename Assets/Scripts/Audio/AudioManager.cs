using UnityEngine;

/// <summary>
/// 音频总管理器：
/// - 统一管理 BGM / SFX 播放；
/// - 直接根据强类型枚举解析配置；
/// - 提供总音量、BGM 音量、SFX 音量控制；
/// - 自动创建运行所需的总线播放器；
/// - 作为跨场景持久对象存在。
/// </summary>
[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    private const string AUDIO_ROOT_OBJECT_NAME = "AudioManager";
    private const string MUSIC_BUS_OBJECT_NAME = "MusicBus";
    private const string SFX_BUS_OBJECT_NAME = "SfxBus";

    private static AudioManager instance;

    [Header("Config")]
    [Tooltip("音效配置总表。所有 SFX 播放请求都会先在这里按枚举查询具体配置。")]
    [SerializeField] private AudioSfxCatalogSO audioSfxCatalog;
    [Tooltip("音频运行时设置。提供状态驱动的 BGM 配置与淡变时长。")]
    [SerializeField] private AudioRuntimeSettingsSO runtimeSettings;

    [Header("Volumes")]
    [SerializeField] [Range(AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME)] private float masterVolume = AudioConstants.DEFAULT_VOLUME;
    [SerializeField] [Range(AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME)] private float musicVolume = AudioConstants.DEFAULT_VOLUME;
    [SerializeField] [Range(AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME)] private float sfxVolume = AudioConstants.DEFAULT_VOLUME;

    private AudioBusPlayer musicBusPlayer;
    private AudioBusPlayer sfxBusPlayer;

    public static AudioManager Instance => instance;
    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;

    private void Awake()
    {
        ValidateConfiguration();

        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureBusPlayers();
        ApplyVolumeSettings();
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<AudioBgmPlayRequestedEvent>(OnAudioBgmPlayRequested);
        GameEventBus.Subscribe<AudioStopRequestedEvent>(OnAudioStopRequested);
        GameEventBus.Subscribe<AudioSfxPlayRequestedEvent>(OnAudioSfxPlayRequested);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<AudioBgmPlayRequestedEvent>(OnAudioBgmPlayRequested);
        GameEventBus.Unsubscribe<AudioStopRequestedEvent>(OnAudioStopRequested);
        GameEventBus.Unsubscribe<AudioSfxPlayRequestedEvent>(OnAudioSfxPlayRequested);
    }

    public static AudioManager EnsureInstance(AudioSfxCatalogSO sfxCatalog, AudioRuntimeSettingsSO settings)
    {
        if (instance != null)
        {
            instance.AssignMissingReferences(sfxCatalog, settings);
            return instance;
        }

        GameObject rootObject = new(AUDIO_ROOT_OBJECT_NAME);
        AudioManager manager = rootObject.AddComponent<AudioManager>();
        manager.audioSfxCatalog = sfxCatalog;
        manager.runtimeSettings = settings;
        return manager;
    }

    /// <summary>
    /// 处理一次 BGM 播放请求。
    /// Music + Loop 会走淡入淡出，其余请求立即执行。
    /// </summary>
    public void PlayBgm(AudioBgmKey bgmKey, bool restartIfPlaying)
    {
        if (!enabled || bgmKey == AudioBgmKey.None)
        {
            return;
        }

        if (!runtimeSettings.TryGetBgmCue(bgmKey, out AudioCueData cueData))
        {
            Debug.LogWarning($"Audio bgm key '{bgmKey}' was not found.", this);
            return;
        }

        if (cueData.PlaybackMode == AudioPlaybackMode.Loop)
        {
            musicBusPlayer.PlayWithFade(cueData, runtimeSettings.MusicFadeDuration, restartIfPlaying);
            return;
        }

        musicBusPlayer.Play(cueData, restartIfPlaying);
    }

    /// <summary>
    /// 处理一次语义音效播放请求。
    /// </summary>
    public void PlaySfx(AudioSfxKey sfxKey)
    {
        if (!enabled || sfxKey == AudioSfxKey.None)
        {
            return;
        }

        if (!audioSfxCatalog.TryGetCue(sfxKey, out AudioCueData cueData))
        {
            Debug.LogWarning($"Audio sfx key '{sfxKey}' was not found.", this);
            return;
        }

        sfxBusPlayer.Play(cueData, false);
    }

    /// <summary>
    /// 停止指定总线。
    /// Music 总线会走淡出，Sfx 总线立即停止。
    /// </summary>
    public void Stop(AudioBusType busType)
    {
        if (!enabled)
        {
            return;
        }

        AudioBusPlayer busPlayer = ResolveBusPlayer(busType);
        if (busType == AudioBusType.Music)
        {
            busPlayer.StopPlaybackWithFade(runtimeSettings.MusicFadeDuration);
            return;
        }

        busPlayer.StopPlayback();
    }

    public bool IsPlayingMusicCue(AudioBgmKey bgmKey)
    {
        return musicBusPlayer.IsPlayingCue(bgmKey.ToString());
    }

    private void ValidateConfiguration()
    {
        if (audioSfxCatalog == null)
        {
            throw new MissingReferenceException($"{nameof(AudioManager)} '{name}' is missing {nameof(AudioSfxCatalogSO)}.");
        }

        if (runtimeSettings == null)
        {
            throw new MissingReferenceException($"{nameof(AudioManager)} '{name}' is missing {nameof(AudioRuntimeSettingsSO)}.");
        }
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp(volume, AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME);
        ApplyVolumeSettings();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp(volume, AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME);
        ApplyVolumeSettings();
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp(volume, AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME);
        ApplyVolumeSettings();
    }

    private void OnAudioBgmPlayRequested(AudioBgmPlayRequestedEvent eventData)
    {
        PlayBgm(eventData.BgmKey, eventData.RestartIfPlaying);
    }

    private void OnAudioStopRequested(AudioStopRequestedEvent eventData)
    {
        Stop(eventData.BusType);
    }

    private void OnAudioSfxPlayRequested(AudioSfxPlayRequestedEvent eventData)
    {
        PlaySfx(eventData.SfxKey);
    }

    private void ApplyVolumeSettings()
    {
        EnsureBusPlayers();
        musicBusPlayer.SetBusVolume(masterVolume * musicVolume);
        sfxBusPlayer.SetBusVolume(masterVolume * sfxVolume);
    }

    private AudioBusPlayer ResolveBusPlayer(AudioBusType busType)
    {
        EnsureBusPlayers();
        return busType == AudioBusType.Music ? musicBusPlayer : sfxBusPlayer;
    }

    private void EnsureBusPlayers()
    {
        musicBusPlayer ??= GetOrCreateBusPlayer(MUSIC_BUS_OBJECT_NAME);
        sfxBusPlayer ??= GetOrCreateBusPlayer(SFX_BUS_OBJECT_NAME);
    }

    private AudioBusPlayer GetOrCreateBusPlayer(string objectName)
    {
        Transform busTransform = transform.Find(objectName);
        if (busTransform == null)
        {
            GameObject busObject = new(objectName);
            busObject.transform.SetParent(transform, false);
            busTransform = busObject.transform;
        }

        AudioBusPlayer busPlayer = busTransform.GetComponent<AudioBusPlayer>();
        if (busPlayer == null)
        {
            busPlayer = busTransform.gameObject.AddComponent<AudioBusPlayer>();
        }

        return busPlayer;
    }

    private void AssignMissingReferences(AudioSfxCatalogSO sfxCatalog, AudioRuntimeSettingsSO settings)
    {
        if (audioSfxCatalog == null)
        {
            audioSfxCatalog = sfxCatalog;
        }

        if (runtimeSettings == null)
        {
            runtimeSettings = settings;
        }
    }
}
