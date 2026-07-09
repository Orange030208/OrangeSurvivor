public interface IAudioService
{
    float MasterVolume { get; }
    float MusicVolume { get; }
    float SfxVolume { get; }

    void PlayBgm(AudioBgmKey bgmKey, bool restartIfPlaying);
    void PlayMusic(AudioBgmKey bgmKey, bool restartIfPlaying);
    void PlaySfx(AudioSfxKey sfxKey);
    void PlaySfx(AudioSfxKey sfxKey, AudioSfxPlayContext context);
    void Stop(AudioBusType busType);
    void StopMusic();
    void StopSfxGroup(string groupId);
    void StopAllSfx();
    void SetSfxGroupVolume(string groupId, float volume);
    bool IsPlayingMusicCue(AudioBgmKey bgmKey);
    void SetMasterVolume(float volume);
    void SetMusicVolume(float volume);
    void SetSfxVolume(float volume);
}
