using System;
using UnityEngine;

namespace JellyRiot.Audio
{
    /// <summary>Inspector-facing audio references and restrained mix defaults.</summary>
    [CreateAssetMenu(fileName = "JellyRiotAudioSettings", menuName = "Jelly Riot/Audio Settings")]
    public sealed class JellyRiotAudioSettings : ScriptableObject
    {
        [Header("UI")]
        [SerializeField] private AudioClip[] uiTaps = new AudioClip[3];
        [SerializeField] private AudioClip uiInvalid;
        [SerializeField] private AudioClip scoreTick;

        [Header("Jellies")]
        [SerializeField] private AudioClip[] drops = new AudioClip[4];
        [SerializeField] private AudioClip[] landings = new AudioClip[3];
        [SerializeField] private AudioClip[] pops = new AudioClip[5];

        [Header("Cascades")]
        [SerializeField] private AudioClip[] cascades = new AudioClip[4];
        [SerializeField] private AudioClip riotStinger;

        [Header("Game")]
        [SerializeField] private AudioClip rowRise;
        [SerializeField] private AudioClip gameOver;
        [SerializeField] private AudioClip bestScore;
        [SerializeField] private AudioClip dailyComplete;

        [Header("Mix")]
        [SerializeField, Range(4, 16)] private int voiceLimit = 10;
        [SerializeField, Range(0f, 1f)] private float uiVolume = 0.34f;
        [SerializeField, Range(0f, 1f)] private float dropVolume = 0.56f;
        [SerializeField, Range(0f, 1f)] private float landingVolume = 0.62f;
        [SerializeField, Range(0f, 1f)] private float popVolume = 0.74f;
        [SerializeField, Range(0f, 1f)] private float cascadeVolume = 0.70f;
        [SerializeField, Range(0f, 1f)] private float riotVolume = 0.82f;
        [SerializeField, Range(0f, 1f)] private float resultVolume = 0.68f;
        [SerializeField, Range(0f, 0.12f)] private float variantPitchRange = 0.035f;

        public AudioClip[] UiTaps => uiTaps;
        public AudioClip UiInvalid => uiInvalid;
        public AudioClip ScoreTick => scoreTick;
        public AudioClip[] Drops => drops;
        public AudioClip[] Landings => landings;
        public AudioClip[] Pops => pops;
        public AudioClip[] Cascades => cascades;
        public AudioClip RiotStinger => riotStinger;
        public AudioClip RowRise => rowRise;
        public AudioClip GameOver => gameOver;
        public AudioClip BestScore => bestScore;
        public AudioClip DailyComplete => dailyComplete;
        public int VoiceLimit => voiceLimit;
        public float UiVolume => uiVolume;
        public float DropVolume => dropVolume;
        public float LandingVolume => landingVolume;
        public float PopVolume => popVolume;
        public float CascadeVolume => cascadeVolume;
        public float RiotVolume => riotVolume;
        public float ResultVolume => resultVolume;
        public float VariantPitchRange => variantPitchRange;

        public bool HasAllRequiredClips =>
            Has(uiTaps, 3) && uiInvalid != null && scoreTick != null && Has(drops, 4) &&
            Has(landings, 3) && Has(pops, 5) && Has(cascades, 4) && riotStinger != null &&
            rowRise != null && gameOver != null && bestScore != null && dailyComplete != null;

        private static bool Has(AudioClip[] clips, int count)
        {
            if (clips == null || clips.Length != count) return false;
            for (int i = 0; i < clips.Length; i++) if (clips[i] == null) return false;
            return true;
        }

#if UNITY_EDITOR
        public void Configure(AudioClip[] tapClips, AudioClip invalidClip, AudioClip tickClip,
            AudioClip[] dropClips, AudioClip[] landingClips, AudioClip[] popClips,
            AudioClip[] cascadeClips, AudioClip riotClip, AudioClip riseClip,
            AudioClip gameOverClip, AudioClip bestClip, AudioClip dailyClip)
        {
            uiTaps = Copy(tapClips, 3, nameof(tapClips));
            uiInvalid = invalidClip;
            scoreTick = tickClip;
            drops = Copy(dropClips, 4, nameof(dropClips));
            landings = Copy(landingClips, 3, nameof(landingClips));
            pops = Copy(popClips, 5, nameof(popClips));
            cascades = Copy(cascadeClips, 4, nameof(cascadeClips));
            riotStinger = riotClip;
            rowRise = riseClip;
            gameOver = gameOverClip;
            bestScore = bestClip;
            dailyComplete = dailyClip;
            if (!HasAllRequiredClips) throw new ArgumentException("All 26 required clips must be assigned.");
        }

        private static AudioClip[] Copy(AudioClip[] clips, int count, string name)
        {
            if (clips == null || clips.Length != count) throw new ArgumentException($"Expected {count} clips.", name);
            return (AudioClip[])clips.Clone();
        }
#endif
    }
}
