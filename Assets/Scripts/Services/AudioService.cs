using System;
using System.Collections.Generic;
using Orange.GameServices;
using UnityEngine;

[Serializable]
public sealed class AudioService : GameService, IAudioService
{
    private const string AUDIO_ROOT_OBJECT_NAME = "Audio";
    private const string MUSIC_BUS_OBJECT_NAME = "MusicBus";
    private const string SFX_BUS_ROOT_OBJECT_NAME = "SfxBuses";

    [Header("配置")]
    [SerializeField] private AudioRuntimeSettingsSO runtimeSettings;
    [SerializeField] private AudioBusSettingsSO busSettings;

    [Header("音量")]
    [SerializeField] [Range(AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME)] private float masterVolume = AudioConstants.DEFAULT_VOLUME;
    [SerializeField] [Range(AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME)] private float musicVolume = AudioConstants.DEFAULT_VOLUME;
    [SerializeField] [Range(AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME)] private float sfxVolume = AudioConstants.DEFAULT_VOLUME;

    private Transform audioRoot;
    private AudioBusPlayer musicBusPlayer;
    private Transform sfxBusRoot;
    private readonly Dictionary<string, AudioSfxGroupBusPlayer> sfxGroupPlayers = new(StringComparer.Ordinal);
    private readonly HashSet<string> warnedMissingGroups = new(StringComparer.Ordinal);
    private bool isRuntimeActive;

    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;

    protected override void RegisterContracts(GameServiceRegistry registry)
    {
        registry.Register<IAudioService>(this);
    }

    protected override void OnValidateService(GameServiceValidationReport report)
    {
        if (runtimeSettings == null)
        {
            report.AddError($"{nameof(AudioService)} requires {nameof(AudioRuntimeSettingsSO)}.", GetType());
        }

        if (busSettings == null)
        {
            report.AddError($"{nameof(AudioService)} requires {nameof(AudioBusSettingsSO)}.", GetType());
        }
    }

    protected override void OnAttach()
    {
        ValidateConfiguration();
        ClampVolumeFields();
        isRuntimeActive = true;

        EnsureBusPlayers();
        ApplyVolumeSettings();

        YokiFrame.EventKit.Type.Register<AudioBgmPlayRequestedEvent>(OnAudioBgmPlayRequested);
        AddCleanup(() => YokiFrame.EventKit.Type.UnRegister<AudioBgmPlayRequestedEvent>(OnAudioBgmPlayRequested));

        YokiFrame.EventKit.Type.Register<AudioMusicPlayRequestedEvent>(OnAudioMusicPlayRequested);
        AddCleanup(() => YokiFrame.EventKit.Type.UnRegister<AudioMusicPlayRequestedEvent>(OnAudioMusicPlayRequested));

        YokiFrame.EventKit.Enum.Register(AudioCommand.MusicStopRequested, OnAudioMusicStopRequested);
        AddCleanup(() => YokiFrame.EventKit.Enum.UnRegister(AudioCommand.MusicStopRequested, OnAudioMusicStopRequested));

        YokiFrame.EventKit.Type.Register<AudioStopRequestedEvent>(OnAudioStopRequested);
        AddCleanup(() => YokiFrame.EventKit.Type.UnRegister<AudioStopRequestedEvent>(OnAudioStopRequested));

        YokiFrame.EventKit.Type.Register<AudioSfxPlayRequestedEvent>(OnAudioSfxPlayRequested);
        AddCleanup(() => YokiFrame.EventKit.Type.UnRegister<AudioSfxPlayRequestedEvent>(OnAudioSfxPlayRequested));

        YokiFrame.EventKit.Type.Register<AudioSfxGroupStopRequestedEvent>(OnAudioSfxGroupStopRequested);
        AddCleanup(() => YokiFrame.EventKit.Type.UnRegister<AudioSfxGroupStopRequestedEvent>(OnAudioSfxGroupStopRequested));

        YokiFrame.EventKit.Type.Register<AudioSfxGroupVolumeChangedEvent>(OnAudioSfxGroupVolumeChanged);
        AddCleanup(() => YokiFrame.EventKit.Type.UnRegister<AudioSfxGroupVolumeChangedEvent>(OnAudioSfxGroupVolumeChanged));
    }

    protected override void OnStart()
    {
        ApplySettingsState(GameSettingsService.Current);
    }

    protected override void OnDispose()
    {
        isRuntimeActive = false;
        StopAllSfx();
        if (musicBusPlayer != null)
        {
            musicBusPlayer.StopPlayback();
        }

        sfxGroupPlayers.Clear();
        warnedMissingGroups.Clear();
        musicBusPlayer = null;
        sfxBusRoot = null;
        if (audioRoot != null)
        {
            UnityEngine.Object.Destroy(audioRoot.gameObject);
            audioRoot = null;
        }
    }

    public void PlayBgm(AudioBgmKey bgmKey, bool restartIfPlaying)
    {
        PlayMusic(bgmKey, restartIfPlaying);
    }

    public void PlayMusic(AudioBgmKey bgmKey, bool restartIfPlaying)
    {
        if (!isRuntimeActive || bgmKey == AudioBgmKey.None)
        {
            return;
        }

        if (!runtimeSettings.TryGetBgmCue(bgmKey, out AudioCueData cueData))
        {
            Debug.LogWarning($"Audio bgm key '{bgmKey}' was not found.", Context?.Root);
            return;
        }

        if (cueData.PlaybackMode == AudioPlaybackMode.Loop)
        {
            musicBusPlayer.PlayWithFade(cueData, runtimeSettings.MusicFadeDuration, restartIfPlaying);
            return;
        }

        musicBusPlayer.Play(cueData, restartIfPlaying);
    }

    public void PlaySfx(AudioSfxKey sfxKey)
    {
        PlaySfx(sfxKey, AudioSfxPlayContext.None);
    }

    public void PlaySfx(AudioSfxKey sfxKey, AudioSfxPlayContext context)
    {
        if (!isRuntimeActive || sfxKey == AudioSfxKey.None)
        {
            return;
        }

        if (busSettings == null || !busSettings.TryGetSfxCue(sfxKey, out AudioCueData cueData))
        {
            Debug.LogWarning($"Audio sfx key '{sfxKey}' was not found.", Context?.Root);
            return;
        }

        AudioSfxGroupBusPlayer groupPlayer = ResolveSfxGroupPlayer(cueData.GroupId);
        groupPlayer.Play(cueData, context);
    }

    public void Stop(AudioBusType busType)
    {
        if (!isRuntimeActive)
        {
            return;
        }

        if (busType == AudioBusType.Music)
        {
            StopMusic();
            return;
        }

        StopAllSfx();
    }

    public void StopMusic()
    {
        if (!isRuntimeActive)
        {
            return;
        }

        musicBusPlayer.StopPlaybackWithFade(runtimeSettings.MusicFadeDuration);
    }

    public void StopSfxGroup(string groupId)
    {
        if (!isRuntimeActive)
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
        return musicBusPlayer != null && musicBusPlayer.IsPlayingCue(bgmKey.ToString());
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

    private void OnAudioMusicStopRequested()
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

    private void ApplySettingsState(GameSettingsState state)
    {
        if (state == null)
        {
            return;
        }

        SetMasterVolume(state.MasterVolume);
        SetSfxVolume(state.SfxVolume);
        SetMusicVolume(state.MusicVolume);
    }

    private void ApplyVolumeSettings()
    {
        if (!isRuntimeActive)
        {
            return;
        }

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

    private void EnsureBusPlayers()
    {
        EnsureAudioRoot();
        musicBusPlayer ??= GetOrCreateBusPlayer(MUSIC_BUS_OBJECT_NAME);
        if (sfxBusRoot == null)
        {
            Transform root = audioRoot.Find(SFX_BUS_ROOT_OBJECT_NAME);
            if (root == null)
            {
                GameObject rootObject = new(SFX_BUS_ROOT_OBJECT_NAME);
                rootObject.transform.SetParent(audioRoot, false);
                root = rootObject.transform;
            }

            sfxBusRoot = root;
        }
    }

    private void EnsureAudioRoot()
    {
        if (audioRoot != null)
        {
            return;
        }

        Transform rootTransform = Context?.RootTransform;
        if (rootTransform == null)
        {
            throw new InvalidOperationException($"{nameof(AudioService)} requires an attached {nameof(GameServiceContext)}.");
        }

        Transform existing = rootTransform.Find(AUDIO_ROOT_OBJECT_NAME);
        if (existing != null)
        {
            audioRoot = existing;
            return;
        }

        GameObject audioRootObject = new(AUDIO_ROOT_OBJECT_NAME);
        audioRootObject.transform.SetParent(rootTransform, false);
        audioRoot = audioRootObject.transform;
    }

    private AudioBusPlayer GetOrCreateBusPlayer(string objectName)
    {
        Transform busTransform = audioRoot.Find(objectName);
        if (busTransform == null)
        {
            GameObject busObject = new(objectName);
            busObject.transform.SetParent(audioRoot, false);
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
            Debug.LogWarning(
                $"Audio sfx group '{groupId}' was not found. Falling back to '{busSettings.DefaultSfxGroupId}'.",
                Context?.Root);
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

    private void ValidateConfiguration()
    {
        if (runtimeSettings == null)
        {
            throw new MissingReferenceException($"{nameof(AudioService)} is missing {nameof(AudioRuntimeSettingsSO)}.");
        }

        if (busSettings == null)
        {
            throw new MissingReferenceException($"{nameof(AudioService)} is missing {nameof(AudioBusSettingsSO)}.");
        }
    }

    private void ClampVolumeFields()
    {
        masterVolume = Mathf.Clamp(masterVolume, AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME);
        musicVolume = Mathf.Clamp(musicVolume, AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME);
        sfxVolume = Mathf.Clamp(sfxVolume, AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME);
    }
}
