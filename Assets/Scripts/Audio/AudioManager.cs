using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音频总管理器：
/// - 统一管理 Music / SFX 播放；
/// - 直接根据强类型枚举解析配置；
/// - 提供总音量、Music 音量、SFX 根音量与 SFX 分组音量控制；
/// - 自动创建运行所需的 Music 总线与 SFX 分组池；
/// - 作为跨场景持久对象存在。
/// </summary>
[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    private const string AUDIO_ROOT_OBJECT_NAME = "AudioManager";
    private const string MUSIC_BUS_OBJECT_NAME = "MusicBus";
    private const string SFX_BUS_ROOT_OBJECT_NAME = "SfxBuses";

    private static AudioManager instance;

    [Header("Config")]
    [Tooltip("音频运行时设置。提供状态驱动的 BGM 配置与淡变时长。")]
    [SerializeField] private AudioRuntimeSettingsSO runtimeSettings;
    [Tooltip("音频总线设置。SFX 分组与各组下的 SFX 条目都在这里配置。")]
    [SerializeField] private AudioBusSettingsSO busSettings;

    [Header("Volumes")]
    [SerializeField] [Range(AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME)] private float masterVolume = AudioConstants.DEFAULT_VOLUME;
    [SerializeField] [Range(AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME)] private float musicVolume = AudioConstants.DEFAULT_VOLUME;
    [SerializeField] [Range(AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME)] private float sfxVolume = AudioConstants.DEFAULT_VOLUME;

    private AudioBusPlayer musicBusPlayer;
    private Transform sfxBusRoot;
    private readonly Dictionary<string, AudioSfxGroupBusPlayer> sfxGroupPlayers = new(StringComparer.Ordinal);
    private readonly HashSet<string> warnedMissingGroups = new(StringComparer.Ordinal);

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
        GameEventBus.Subscribe<AudioMusicPlayRequestedEvent>(OnAudioMusicPlayRequested);
        GameEventBus.Subscribe<AudioMusicStopRequestedEvent>(OnAudioMusicStopRequested);
        GameEventBus.Subscribe<AudioStopRequestedEvent>(OnAudioStopRequested);
        GameEventBus.Subscribe<AudioSfxPlayRequestedEvent>(OnAudioSfxPlayRequested);
        GameEventBus.Subscribe<AudioSfxGroupStopRequestedEvent>(OnAudioSfxGroupStopRequested);
        GameEventBus.Subscribe<AudioSfxGroupVolumeChangedEvent>(OnAudioSfxGroupVolumeChanged);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<AudioBgmPlayRequestedEvent>(OnAudioBgmPlayRequested);
        GameEventBus.Unsubscribe<AudioMusicPlayRequestedEvent>(OnAudioMusicPlayRequested);
        GameEventBus.Unsubscribe<AudioMusicStopRequestedEvent>(OnAudioMusicStopRequested);
        GameEventBus.Unsubscribe<AudioStopRequestedEvent>(OnAudioStopRequested);
        GameEventBus.Unsubscribe<AudioSfxPlayRequestedEvent>(OnAudioSfxPlayRequested);
        GameEventBus.Unsubscribe<AudioSfxGroupStopRequestedEvent>(OnAudioSfxGroupStopRequested);
        GameEventBus.Unsubscribe<AudioSfxGroupVolumeChangedEvent>(OnAudioSfxGroupVolumeChanged);
    }

    private void OnValidate()
    {
        masterVolume = Mathf.Clamp(masterVolume, AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME);
        musicVolume = Mathf.Clamp(musicVolume, AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME);
        sfxVolume = Mathf.Clamp(sfxVolume, AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME);

        if (!Application.isPlaying || instance != this)
        {
            return;
        }

        ApplyVolumeSettings();
    }

    public static AudioManager EnsureInstance(AudioRuntimeSettingsSO settings)
    {
        return EnsureInstance(settings, null);
    }

    public static AudioManager EnsureInstance(AudioRuntimeSettingsSO settings, AudioBusSettingsSO busSettings)
    {
        if (instance != null)
        {
            instance.AssignMissingReferences(settings, busSettings);
            return instance;
        }

        GameObject rootObject = new(AUDIO_ROOT_OBJECT_NAME);
        AudioManager manager = rootObject.AddComponent<AudioManager>();
        manager.runtimeSettings = settings;
        manager.busSettings = busSettings;
        return manager;
    }

    /// <summary>
    /// 处理一次 BGM 播放请求。
    /// Music + Loop 会走淡入淡出，其余请求立即执行。
    /// </summary>
    public void PlayBgm(AudioBgmKey bgmKey, bool restartIfPlaying)
    {
        PlayMusic(bgmKey, restartIfPlaying);
    }

    public void PlayMusic(AudioBgmKey bgmKey, bool restartIfPlaying)
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
        PlaySfx(sfxKey, AudioSfxPlayContext.None);
    }

    public void PlaySfx(AudioSfxKey sfxKey, AudioSfxPlayContext context)
    {
        if (!enabled || sfxKey == AudioSfxKey.None)
        {
            return;
        }

        if (busSettings == null || !busSettings.TryGetSfxCue(sfxKey, out AudioCueData cueData))
        {
            Debug.LogWarning($"Audio sfx key '{sfxKey}' was not found.", this);
            return;
        }

        AudioSfxGroupBusPlayer groupPlayer = ResolveSfxGroupPlayer(cueData.GroupId);
        groupPlayer.Play(cueData, context);
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
            StopMusic();
            return;
        }

        StopAllSfx();
    }

    public void StopMusic()
    {
        if (!enabled)
        {
            return;
        }

        musicBusPlayer.StopPlaybackWithFade(runtimeSettings.MusicFadeDuration);
    }

    public void StopSfxGroup(string groupId)
    {
        if (!enabled)
        {
            return;
        }

        AudioSfxGroupBusPlayer groupPlayer = ResolveSfxGroupPlayer(groupId);
        groupPlayer.StopAll();
    }

    public void StopAllSfx()
    {
        foreach (AudioSfxGroupBusPlayer groupPlayer in sfxGroupPlayers.Values)
        {
            if (groupPlayer != null)
            {
                groupPlayer.StopAll();
            }
        }
    }

    public void SetSfxGroupVolume(string groupId, float volume)
    {
        AudioSfxGroupBusPlayer groupPlayer = ResolveSfxGroupPlayer(groupId);
        groupPlayer.SetGroupVolume(volume);
    }

    public bool IsPlayingMusicCue(AudioBgmKey bgmKey)
    {
        return musicBusPlayer.IsPlayingCue(bgmKey.ToString());
    }

    private void ValidateConfiguration()
    {
        if (runtimeSettings == null)
        {
            throw new MissingReferenceException($"{nameof(AudioManager)} '{name}' is missing {nameof(AudioRuntimeSettingsSO)}.");
        }

        if (busSettings == null)
        {
            throw new MissingReferenceException($"{nameof(AudioManager)} '{name}' is missing {nameof(AudioBusSettingsSO)}.");
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
        PlayMusic(eventData.BgmKey, eventData.RestartIfPlaying);
    }

    private void OnAudioMusicPlayRequested(AudioMusicPlayRequestedEvent eventData)
    {
        PlayMusic(eventData.BgmKey, eventData.RestartIfPlaying);
    }

    private void OnAudioMusicStopRequested(AudioMusicStopRequestedEvent eventData)
    {
        StopMusic();
    }

    private void OnAudioStopRequested(AudioStopRequestedEvent eventData)
    {
        Stop(eventData.BusType);
    }

    private void OnAudioSfxPlayRequested(AudioSfxPlayRequestedEvent eventData)
    {
        AudioSfxPlayContext context = eventData.HasPosition
            ? new AudioSfxPlayContext(eventData.Position)
            : AudioSfxPlayContext.None;
        PlaySfx(eventData.SfxKey, context);
    }

    private void OnAudioSfxGroupStopRequested(AudioSfxGroupStopRequestedEvent eventData)
    {
        StopSfxGroup(eventData.GroupId);
    }

    private void OnAudioSfxGroupVolumeChanged(AudioSfxGroupVolumeChangedEvent eventData)
    {
        SetSfxGroupVolume(eventData.GroupId, eventData.Volume);
    }

    private void ApplyVolumeSettings()
    {
        EnsureBusPlayers();
        musicBusPlayer.SetBusVolume(masterVolume * musicVolume * GetBusMusicVolume());

        float rootSfxVolume = masterVolume * sfxVolume * GetBusSfxVolume();
        foreach (AudioSfxGroupBusPlayer groupPlayer in sfxGroupPlayers.Values)
        {
            if (groupPlayer != null)
            {
                groupPlayer.SetRootVolume(rootSfxVolume);
            }
        }
    }

    private AudioBusPlayer ResolveBusPlayer(AudioBusType busType)
    {
        EnsureBusPlayers();
        return musicBusPlayer;
    }

    private void EnsureBusPlayers()
    {
        musicBusPlayer ??= GetOrCreateBusPlayer(MUSIC_BUS_OBJECT_NAME);
        if (sfxBusRoot == null)
        {
            Transform root = transform.Find(SFX_BUS_ROOT_OBJECT_NAME);
            if (root == null)
            {
                GameObject rootObject = new(SFX_BUS_ROOT_OBJECT_NAME);
                rootObject.transform.SetParent(transform, false);
                root = rootObject.transform;
            }

            sfxBusRoot = root;
        }
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

    private AudioSfxGroupBusPlayer ResolveSfxGroupPlayer(string requestedGroupId)
    {
        EnsureBusPlayers();

        string groupId = AudioBusSettingsSO.NormalizeGroupId(requestedGroupId);
        AudioSfxGroupSettings groupSettings = ResolveSfxGroupSettings(groupId);
        groupId = groupSettings.GroupId;

        if (sfxGroupPlayers.TryGetValue(groupId, out AudioSfxGroupBusPlayer existing) && existing != null)
        {
            return existing;
        }

        Transform groupTransform = sfxBusRoot.Find(groupId);
        if (groupTransform == null)
        {
            GameObject groupObject = new(groupId);
            groupObject.transform.SetParent(sfxBusRoot, false);
            groupTransform = groupObject.transform;
        }

        AudioSfxGroupBusPlayer groupPlayer = groupTransform.GetComponent<AudioSfxGroupBusPlayer>();
        if (groupPlayer == null)
        {
            groupPlayer = groupTransform.gameObject.AddComponent<AudioSfxGroupBusPlayer>();
        }

        groupPlayer.Configure(groupSettings, masterVolume * sfxVolume * GetBusSfxVolume(), ResolveAudibleDistance());
        sfxGroupPlayers[groupId] = groupPlayer;
        return groupPlayer;
    }

    private AudioSfxGroupSettings ResolveSfxGroupSettings(string groupId)
    {
        if (busSettings.TryGetSfxGroup(groupId, out AudioSfxGroupSettings settings))
        {
            return settings;
        }

        if (warnedMissingGroups.Add(groupId))
        {
            Debug.LogWarning($"Audio sfx group '{groupId}' was not found. Falling back to '{busSettings.DefaultSfxGroupId}'.", this);
        }

        return busSettings.GetDefaultSfxGroup();
    }

    private float GetBusMusicVolume()
    {
        return busSettings.MusicVolume;
    }

    private float GetBusSfxVolume()
    {
        return busSettings.SfxVolume;
    }

    private float ResolveAudibleDistance()
    {
        return busSettings.AudibleDistance;
    }

    private void AssignMissingReferences(AudioRuntimeSettingsSO settings, AudioBusSettingsSO busSettings)
    {
        if (runtimeSettings == null)
        {
            runtimeSettings = settings;
        }

        if (this.busSettings == null)
        {
            this.busSettings = busSettings;
        }
    }
}
