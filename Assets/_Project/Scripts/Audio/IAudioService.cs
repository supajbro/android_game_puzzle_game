namespace JellyRiot.Audio
{
    public interface IAudioService
    {
        bool IsMuted { get; set; }
        float MasterVolume { get; set; }
        float SfxVolume { get; set; }
        float MusicVolume { get; set; }
        void SetPaused(bool paused);
        void SetExternalInterruption(bool interrupted);
        void StopPendingGameplayAudio();
    }
}
