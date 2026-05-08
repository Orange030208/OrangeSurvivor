using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class AudioSfxGroupBusPlayer : MonoBehaviour
{
    private readonly List<AudioSource> sourcePool = new();
    private readonly Dictionary<AudioSource, ActiveSfxPlayback> activeBySource = new();
    private readonly Dictionary<string, int> activeCountByCue = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> lastPlayTimeByCue = new(StringComparer.Ordinal);

    private string groupId;
    private int maxConcurrent;
    private int groupPriority;
    private bool spatializeByDefault;
    private float groupVolume = AudioConstants.DEFAULT_VOLUME;
    private float rootVolume = AudioConstants.DEFAULT_VOLUME;
    private float audibleDistance = AudioConstants.DEFAULT_2D_AUDIBLE_DISTANCE;

    public string GroupId => groupId;
    public int ActiveCount => activeBySource.Count;

    public void Configure(AudioSfxGroupSettings settings, float rootVolume, float audibleDistance)
    {
        if (settings == null)
        {
            settings = new AudioSfxGroupSettings(AudioConstants.DEFAULT_SFX_GROUP_ID, AudioConstants.DEFAULT_VOLUME,
                AudioConstants.DEFAULT_SFX_POOL_SIZE, AudioConstants.DEFAULT_SFX_MAX_CONCURRENT,
                AudioConstants.DEFAULT_AUDIO_PRIORITY, false);
        }

        groupId = settings.GroupId;
        groupVolume = settings.Volume;
        maxConcurrent = settings.MaxConcurrent;
        groupPriority = settings.Priority;
        spatializeByDefault = settings.SpatializeByDefault;
        this.rootVolume = Mathf.Clamp(rootVolume, AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME);
        this.audibleDistance = Mathf.Max(AudioConstants.MIN_2D_AUDIBLE_DISTANCE, audibleDistance);
        EnsurePoolSize(settings.PoolSize);
        ApplyVolumeToActiveSources();
    }

    private void Update()
    {
        ReclaimFinishedSources();
    }

    public void SetRootVolume(float volume)
    {
        rootVolume = Mathf.Clamp(volume, AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME);
        ApplyVolumeToActiveSources();
    }

    public void SetGroupVolume(float volume)
    {
        groupVolume = Mathf.Clamp(volume, AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME);
        ApplyVolumeToActiveSources();
    }

    public void Play(AudioCueData cueData, AudioSfxPlayContext context)
    {
        ReclaimFinishedSources();

        if (!CanPlay(cueData))
        {
            return;
        }

        AudioSource source = GetAvailableSource(cueData);
        if (source == null)
        {
            return;
        }

        AudioClip selectedClip = cueData.SelectClip();
        if (selectedClip == null)
        {
            return;
        }

        float volumeMultiplier = Resolve2DVolumeMultiplier(cueData, context);
        if (volumeMultiplier <= 0f)
        {
            return;
        }

        ConfigureSource(source, cueData, selectedClip, volumeMultiplier, context);
        RegisterActiveSource(source, cueData, volumeMultiplier);
        source.Play();
    }

    public void StopAll()
    {
        foreach (AudioSource source in sourcePool)
        {
            if (source == null)
            {
                continue;
            }

            source.Stop();
            source.clip = null;
            source.gameObject.SetActive(false);
        }

        activeBySource.Clear();
        activeCountByCue.Clear();
    }

    private bool CanPlay(AudioCueData cueData)
    {
        if (activeBySource.Count >= maxConcurrent && !CanStealSource(cueData.Priority))
        {
            return false;
        }

        if (activeCountByCue.TryGetValue(cueData.CueId, out int activeCueCount)
            && activeCueCount >= cueData.MaxConcurrent
            && !CanStealCueSource(cueData))
        {
            return false;
        }

        if (cueData.Cooldown > 0f
            && lastPlayTimeByCue.TryGetValue(cueData.CueId, out float lastPlayTime)
            && Time.unscaledTime - lastPlayTime < cueData.Cooldown)
        {
            return false;
        }

        return true;
    }

    private AudioSource GetAvailableSource(AudioCueData cueData)
    {
        if (activeCountByCue.TryGetValue(cueData.CueId, out int activeCueCount) && activeCueCount >= cueData.MaxConcurrent)
        {
            AudioSource cueStealCandidate = FindStealCandidate(cueData.Priority, cueData.CueId);
            if (cueStealCandidate == null)
            {
                return null;
            }

            ReleaseSource(cueStealCandidate);
            return cueStealCandidate;
        }

        for (int i = 0; i < sourcePool.Count; i++)
        {
            AudioSource source = sourcePool[i];
            if (source != null && !source.isPlaying && !activeBySource.ContainsKey(source))
            {
                return source;
            }
        }

        AudioSource stealCandidate = FindStealCandidate(cueData.Priority, null);
        if (stealCandidate == null)
        {
            return null;
        }

        ReleaseSource(stealCandidate);
        return stealCandidate;
    }

    private bool CanStealSource(int priority)
    {
        return FindStealCandidate(priority, null) != null;
    }

    private bool CanStealCueSource(AudioCueData cueData)
    {
        return FindStealCandidate(cueData.Priority, cueData.CueId) != null;
    }

    private AudioSource FindStealCandidate(int incomingPriority, string cueId)
    {
        AudioSource candidate = null;
        ActiveSfxPlayback candidatePlayback = default;
        foreach (KeyValuePair<AudioSource, ActiveSfxPlayback> pair in activeBySource)
        {
            ActiveSfxPlayback playback = pair.Value;
            if (!string.IsNullOrEmpty(cueId) && playback.CueId != cueId)
            {
                continue;
            }

            if (playback.Priority < incomingPriority)
            {
                continue;
            }

            if (candidate == null || playback.Priority > candidatePlayback.Priority || playback.StartTime < candidatePlayback.StartTime)
            {
                candidate = pair.Key;
                candidatePlayback = playback;
            }
        }

        return candidate;
    }

    private void ConfigureSource(AudioSource source, AudioCueData cueData, AudioClip selectedClip, float volumeMultiplier, AudioSfxPlayContext context)
    {
        source.gameObject.SetActive(true);
        source.Stop();
        source.clip = selectedClip;
        source.loop = false;
        source.pitch = cueData.SelectPitch();
        source.priority = Mathf.Min(cueData.Priority, groupPriority);
        source.spatialBlend = 0f;
        source.volume = GetSourceVolume(cueData.VolumeScale, volumeMultiplier);
        source.panStereo = Resolve2DPan(cueData, context);
        source.playOnAwake = false;
    }

    private float Resolve2DVolumeMultiplier(AudioCueData cueData, AudioSfxPlayContext context)
    {
        if (!ShouldApply2DSpatial(cueData, context))
        {
            return 1f;
        }

        Transform listener = ResolveListenerTransform();
        if (listener == null)
        {
            return 1f;
        }

        float distance = Vector2.Distance(context.Position, listener.position);
        return Mathf.Clamp01(1f - distance / audibleDistance);
    }

    private float Resolve2DPan(AudioCueData cueData, AudioSfxPlayContext context)
    {
        if (!ShouldApply2DSpatial(cueData, context))
        {
            return 0f;
        }

        Transform listener = ResolveListenerTransform();
        if (listener == null)
        {
            return 0f;
        }

        float deltaX = context.Position.x - listener.position.x;
        return Mathf.Clamp(deltaX / audibleDistance, -1f, 1f);
    }

    private bool ShouldApply2DSpatial(AudioCueData cueData, AudioSfxPlayContext context)
    {
        return context.HasPosition && (cueData.Use2DSpatialBlend || spatializeByDefault);
    }

    private Transform ResolveListenerTransform()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            return mainCamera.transform;
        }

        AudioListener listener = FindFirstObjectByType<AudioListener>();
        return listener != null ? listener.transform : null;
    }

    private float GetSourceVolume(float cueVolumeScale, float volumeMultiplier)
    {
        return Mathf.Clamp01(rootVolume * groupVolume * cueVolumeScale * volumeMultiplier);
    }

    private void RegisterActiveSource(AudioSource source, AudioCueData cueData, float volumeMultiplier)
    {
        activeBySource[source] = new ActiveSfxPlayback(cueData.CueId, cueData.Priority, Time.unscaledTime, cueData.VolumeScale, volumeMultiplier);
        activeCountByCue.TryGetValue(cueData.CueId, out int currentCount);
        activeCountByCue[cueData.CueId] = currentCount + 1;
        lastPlayTimeByCue[cueData.CueId] = Time.unscaledTime;
    }

    private void ReclaimFinishedSources()
    {
        for (int i = sourcePool.Count - 1; i >= 0; i--)
        {
            AudioSource source = sourcePool[i];
            if (source == null || !activeBySource.ContainsKey(source) || source.isPlaying)
            {
                continue;
            }

            ReleaseSource(source);
        }
    }

    private void ReleaseSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        if (activeBySource.TryGetValue(source, out ActiveSfxPlayback playback))
        {
            activeBySource.Remove(source);
            if (activeCountByCue.TryGetValue(playback.CueId, out int count))
            {
                count--;
                if (count <= 0)
                {
                    activeCountByCue.Remove(playback.CueId);
                }
                else
                {
                    activeCountByCue[playback.CueId] = count;
                }
            }
        }

        source.Stop();
        source.clip = null;
        source.gameObject.SetActive(false);
    }

    private void ApplyVolumeToActiveSources()
    {
        foreach (KeyValuePair<AudioSource, ActiveSfxPlayback> pair in activeBySource)
        {
            if (pair.Key != null)
            {
                pair.Key.volume = GetSourceVolume(pair.Value.VolumeScale, pair.Value.VolumeMultiplier);
            }
        }
    }

    private void EnsurePoolSize(int poolSize)
    {
        int clampedPoolSize = Mathf.Clamp(poolSize, AudioConstants.MIN_POOL_SIZE, AudioConstants.MAX_POOL_SIZE);
        while (sourcePool.Count < clampedPoolSize)
        {
            GameObject sourceObject = new($"{nameof(AudioSfxGroupBusPlayer)}_{GroupId}_{sourcePool.Count}");
            sourceObject.transform.SetParent(transform, false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            sourceObject.SetActive(false);
            sourcePool.Add(source);
        }
    }

    private readonly struct ActiveSfxPlayback
    {
        public string CueId { get; }
        public int Priority { get; }
        public float StartTime { get; }
        public float VolumeScale { get; }
        public float VolumeMultiplier { get; }

        public ActiveSfxPlayback(string cueId, int priority, float startTime, float volumeScale, float volumeMultiplier)
        {
            CueId = cueId;
            Priority = priority;
            StartTime = startTime;
            VolumeScale = volumeScale;
            VolumeMultiplier = volumeMultiplier;
        }
    }
}
