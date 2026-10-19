namespace JellyRiot.Platform
{
    public enum HapticCue
    {
        Landing = 0,
        Match = 1,
        Riot = 2
    }

    public interface IHapticsService
    {
        bool IsEnabled { get; set; }
        bool IsSupported { get; }
        void Play(HapticCue cue);
    }
}
