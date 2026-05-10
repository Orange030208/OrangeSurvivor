using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单音频总线播放器：
/// - 负责驱动一个 AudioSource；
/// - 负责 OneShot / Loop 的基础播放；
/// - 负责 BGM 的淡入淡出切换；
/// - 负责叠加总线音量。
/// 它只处理“如何播”，不决定“何时播什么”。
/// </summary>
[DisallowMultipleComponent]
public class AudioBusPlayer : MonoBehaviour
{
    [Tooltip("当前总线对应的主音源组件。音乐总线使用该源播放循环内容；音效总线可复用它生成临时一次性播放源。")]
    [SerializeField] private AudioSource audioSource;

    private Tween volumeTween;
    private float busVolume = AudioConstants.DEFAULT_VOLUME;
    private string currentCueId;
    private readonly List<AudioSource> activeOneShotSources = new();

    public bool IsPlaying => audioSource.isPlaying;
    public string CurrentCueId => currentCueId;

    private void Awake()
    {
        EnsureAudioSource();
    }

    private void OnDestroy()
    {
        volumeTween?.Kill();
        activeOneShotSources.Clear();
    }

    public bool IsPlayingCue(string cueId)
    {
        return !string.IsNullOrWhiteSpace(cueId)
               && !string.IsNullOrWhiteSpace(currentCueId)
               && string.Equals(currentCueId, cueId, System.StringComparison.Ordinal);
    }

    /// <summary>
    /// 立即播放一个 cue。
    /// OneShot 会直接叠加到当前总线；Loop 会替换当前循环内容。
    /// </summary>
    public void Play(AudioCueData cueData, bool restartIfPlaying)
    {
        AudioSource source = audioSource;

        if (cueData.PlaybackMode == AudioPlaybackMode.OneShot)
        {
            PlayOneShot(cueData);
            return;
        }

        if (source.isPlaying && !restartIfPlaying && currentCueId == cueData.CueId)
        {
            return;
        }

        ApplyLoopCue(source, cueData);
    }

    /// <summary>
    /// 以淡出旧曲、淡入新曲的方式播放循环 cue。
    /// 非循环 cue 会退化为立即播放。
    /// </summary>
    public void PlayWithFade(AudioCueData cueData, float fadeDuration, bool restartIfPlaying)
    {
        AudioSource source = audioSource;

        if (cueData.PlaybackMode != AudioPlaybackMode.Loop)
        {
            Play(cueData, restartIfPlaying);
            return;
        }

        float clampedDuration = Mathf.Max(AudioConstants.MIN_FADE_DURATION, fadeDuration);
        if (!source.isPlaying || string.IsNullOrEmpty(currentCueId))
        {
            ApplyLoopCue(source, cueData);
            FadeToVolume(source, GetTargetVolume(), clampedDuration);
            return;
        }

        if (currentCueId == cueData.CueId)
        {
            if (!restartIfPlaying)
            {
                FadeToVolume(source, GetTargetVolume(), clampedDuration);
                return;
            }
        }

        volumeTween?.Kill();
        volumeTween = source.DOFade(AudioConstants.MIN_FADE_DURATION, clampedDuration)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                ApplyLoopCue(source, cueData);
                source.volume = AudioConstants.MIN_FADE_DURATION;
                FadeToVolume(source, GetTargetVolume(), clampedDuration);
            });
    }

    /// <summary>
    /// 立即停止当前总线播放内容。
    /// </summary>
    public void StopPlayback()
    {
        AudioSource source = audioSource;

        volumeTween?.Kill();
        source.Stop();
        source.clip = null;
        currentCueId = null;
        StopActiveOneShotSources();
    }

    /// <summary>
    /// 淡出后停止当前总线播放内容。
    /// </summary>
    public void StopPlaybackWithFade(float fadeDuration)
    {
        AudioSource source = audioSource;

        if (!source.isPlaying)
        {
            StopPlayback();
            return;
        }

        float clampedDuration = Mathf.Max(AudioConstants.MIN_FADE_DURATION, fadeDuration);
        volumeTween?.Kill();
        volumeTween = source.DOFade(AudioConstants.MIN_FADE_DURATION, clampedDuration)
            .SetUpdate(true)
            .OnComplete(StopPlayback);
    }

    /// <summary>
    /// 设置该总线的运行时音量。
    /// </summary>
    public void SetBusVolume(float volume)
    {
        AudioSource source = audioSource;
        busVolume = Mathf.Clamp(volume, AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME);
        UpdateActiveOneShotVolumes();
        if (!source.isPlaying)
        {
            return;
        }

        source.volume = GetTargetVolume();
    }

    private void PlayOneShot(AudioCueData cueData)
    {
        GameObject tempObject = new($"{nameof(AudioBusPlayer)}_{cueData.CueId}");
        tempObject.transform.SetParent(transform, false);

        AudioSource oneShotSource = tempObject.AddComponent<AudioSource>();
        CopyOneShotSettings(oneShotSource);
        oneShotSource.clip = cueData.Clip;
        oneShotSource.volume = GetTargetVolume();
        oneShotSource.pitch = cueData.Pitch;
        oneShotSource.loop = false;
        oneShotSource.Play();
        activeOneShotSources.Add(oneShotSource);

        float clipDuration = Mathf.Max(0.01f, cueData.Clip.length / Mathf.Max(0.01f, Mathf.Abs(cueData.Pitch)));
        Destroy(tempObject, clipDuration);
    }

    private void CopyOneShotSettings(AudioSource targetSource)
    {
        targetSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
        targetSource.priority = audioSource.priority;
        targetSource.panStereo = audioSource.panStereo;
        targetSource.spatialBlend = audioSource.spatialBlend;
        targetSource.reverbZoneMix = audioSource.reverbZoneMix;
        targetSource.bypassEffects = audioSource.bypassEffects;
        targetSource.bypassListenerEffects = audioSource.bypassListenerEffects;
        targetSource.bypassReverbZones = audioSource.bypassReverbZones;
        targetSource.ignoreListenerPause = audioSource.ignoreListenerPause;
        targetSource.ignoreListenerVolume = audioSource.ignoreListenerVolume;
        targetSource.playOnAwake = false;
    }

    private void ApplyLoopCue(AudioSource source, AudioCueData cueData)
    {
        volumeTween?.Kill();
        currentCueId = cueData.CueId;
        source.loop = true;
        source.clip = cueData.Clip;
        source.pitch = cueData.Pitch;
        source.volume = GetTargetVolume();
        source.Play();
    }

    private void FadeToVolume(AudioSource source, float targetVolume, float fadeDuration)
    {
        volumeTween?.Kill();
        if (fadeDuration <= AudioConstants.MIN_FADE_DURATION)
        {
            source.volume = targetVolume;
            return;
        }

        volumeTween = source.DOFade(targetVolume, fadeDuration).SetUpdate(true);
    }

    private float GetTargetVolume()
    {
        return busVolume;
    }

    private void UpdateActiveOneShotVolumes()
    {
        for (int i = activeOneShotSources.Count - 1; i >= 0; i--)
        {
            AudioSource source = activeOneShotSources[i];
            if (source == null || !source.isPlaying)
            {
                activeOneShotSources.RemoveAt(i);
                continue;
            }

            source.volume = GetTargetVolume();
        }
    }

    private void StopActiveOneShotSources()
    {
        for (int i = activeOneShotSources.Count - 1; i >= 0; i--)
        {
            AudioSource source = activeOneShotSources[i];
            if (source != null)
            {
                Destroy(source.gameObject);
            }
        }

        activeOneShotSources.Clear();
    }

    private void EnsureAudioSource()
    {
        if (audioSource != null)
        {
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }
}
