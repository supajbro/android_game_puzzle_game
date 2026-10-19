using System;
using System.Collections.Generic;
using JellyRiot.Core;
using JellyRiot.Platform;
using UnityEngine;

namespace JellyRiot.Audio
{
    /// <summary>
    /// Pooled 2D audio presentation. Core events are consumed once and cosmetic randomness is
    /// deliberately isolated from deterministic gameplay randomness.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour, IAudioService
    {
        private const string ResourceName = "JellyRiotAudioSettings";
        private const string MutedKey = "JellyRiot.Settings.AudioMuted";
        private const string MasterKey = "JellyRiot.Settings.MasterVolume";
        private const string SfxKey = "JellyRiot.Settings.SfxVolume";
        private const string MusicKey = "JellyRiot.Settings.MusicVolume";
        private const float ScoreTickInterval = 0.075f;
        private const float WaveInterval = 0.105f;

        private struct ScheduledSound
        {
            public float At;
            public AudioClip Clip;
            public float Volume;
            public float Pitch;
            public bool Major;
        }

        [SerializeField] private JellyRiotAudioSettings settings;
        private AudioSource[] _voices;
        private AudioSource _musicSource;
        private readonly List<ScheduledSound> _scheduled = new List<ScheduledSound>(16);
        private System.Random _cosmeticRandom;
        private IHapticsService _haptics;
        private int _lastEventSequence;
        private int _lastLandingSequence;
        private int _nextVoice;
        private float _duckUntil;
        private float _masterVolume;
        private float _sfxVolume;
        private float _musicVolume;
        private bool _muted;
        private bool _paused;
        private bool _interrupted;

        public JellyRiotAudioSettings Settings => settings;
        public int VoiceCount => _voices == null ? 0 : _voices.Length;
        public int PendingSoundCount => _scheduled.Count;
        public int IssuedSoundCount { get; private set; }
        public int LastProcessedEventSequence => _lastEventSequence;
        public bool IsPlaybackPaused => _paused || _interrupted;
        public IHapticsService Haptics => _haptics;

        public bool IsMuted
        {
            get => _muted;
            set
            {
                if (_muted == value) return;
                _muted = value;
                PlayerPrefs.SetInt(MutedKey, value ? 1 : 0);
                SaveAndApplyVolumes();
                if (value) StopPendingGameplayAudio();
            }
        }

        public float MasterVolume
        {
            get => _masterVolume;
            set { _masterVolume = Mathf.Clamp01(value); PlayerPrefs.SetFloat(MasterKey, _masterVolume); SaveAndApplyVolumes(); }
        }

        public float SfxVolume
        {
            get => _sfxVolume;
            set { _sfxVolume = Mathf.Clamp01(value); PlayerPrefs.SetFloat(SfxKey, _sfxVolume); SaveAndApplyVolumes(); }
        }

        public float MusicVolume
        {
            get => _musicVolume;
            set { _musicVolume = Mathf.Clamp01(value); PlayerPrefs.SetFloat(MusicKey, _musicVolume); SaveAndApplyVolumes(); }
        }

        private void Awake()
        {
            if (settings == null) settings = Resources.Load<JellyRiotAudioSettings>(ResourceName);
            if (settings == null)
            {
                enabled = false;
                return;
            }

            _masterVolume = PlayerPrefs.GetFloat(MasterKey, 1f);
            _sfxVolume = PlayerPrefs.GetFloat(SfxKey, 1f);
            _musicVolume = PlayerPrefs.GetFloat(MusicKey, 0.25f);
            _muted = PlayerPrefs.GetInt(MutedKey, 0) != 0;
            _cosmeticRandom = new System.Random(unchecked(Environment.TickCount ^ GetInstanceID()));
            _haptics = new AndroidHapticsService();
            BuildPool();
            ApplyVolumes();
        }

        private void Update()
        {
            if (_paused || _interrupted || _muted) return;
            float now = Time.unscaledTime;
            for (int i = _scheduled.Count - 1; i >= 0; i--)
            {
                ScheduledSound sound = _scheduled[i];
                if (sound.At > now) continue;
                _scheduled.RemoveAt(i);
                Play(sound.Clip, sound.Volume, sound.Pitch, sound.Major);
            }
            ApplyVolumes();
        }

        public void ResetEventTracking()
        {
            _lastEventSequence = 0;
            _lastLandingSequence = 0;
            StopPendingGameplayAudio();
        }

        public void ProcessAction(SessionActionResult action)
        {
            if (action == null || settings == null) return;
            float delay = 0f;
            bool riotPlayed = false;
            int waveEventOrdinal = 0;
            for (int i = 0; i < action.Events.Count; i++)
            {
                SessionEvent sessionEvent = action.Events[i];
                if (sessionEvent.Sequence <= _lastEventSequence) continue;
                _lastEventSequence = sessionEvent.Sequence;
                switch (sessionEvent.Type)
                {
                    case SessionEventType.RackSlotSelected:
                        PlayVariant(settings.UiTaps, settings.UiVolume);
                        break;
                    case SessionEventType.ActionRejected:
                        Play(settings.UiInvalid, settings.UiVolume, 1f);
                        break;
                    case SessionEventType.JellyPlaced:
                        PlayVariant(settings.Drops, settings.DropVolume);
                        break;
                    case SessionEventType.WaveResolved:
                        ScheduleWave(action, waveEventOrdinal++, sessionEvent.WaveIndex, ref delay);
                        if (sessionEvent.WaveIndex >= 2 && !riotPlayed)
                        {
                            Schedule(settings.RiotStinger, settings.RiotVolume, 1f, delay, true);
                            _haptics.Play(HapticCue.Riot);
                            riotPlayed = true;
                            delay += 0.08f;
                        }
                        break;
                    case SessionEventType.ScoreChanged:
                        Schedule(settings.ScoreTick, settings.UiVolume, 1f, delay + ScoreTickInterval);
                        break;
                    case SessionEventType.PressureRowApplied:
                        Schedule(settings.RowRise, settings.ResultVolume, 1f, delay);
                        break;
                    case SessionEventType.RunFinalised:
                        Schedule(settings.GameOver, settings.ResultVolume, 1f, delay);
                        if (FindCue(action, PresentationCue.PersonalBest))
                            Schedule(settings.BestScore, settings.ResultVolume, 1f, delay + 0.24f);
                        break;
                }
            }
        }

        public void PlayUiTap()
        {
            if (settings != null) PlayVariant(settings.UiTaps, settings.UiVolume);
        }

        public void PlayLandingOnce(int placementSequence)
        {
            if (settings == null || placementSequence <= _lastLandingSequence) return;
            _lastLandingSequence = placementSequence;
            PlayVariant(settings.Landings, settings.LandingVolume);
            _haptics.Play(HapticCue.Landing);
        }

        public void SetPaused(bool paused)
        {
            if (_paused == paused) return;
            _paused = paused;
            if (paused)
            {
                StopPendingGameplayAudio();
                PauseSources();
            }
            else if (!_interrupted)
            {
                ResumeMusicOnly();
            }
        }

        public void SetExternalInterruption(bool interrupted)
        {
            if (_interrupted == interrupted) return;
            _interrupted = interrupted;
            StopPendingGameplayAudio();
            if (interrupted) PauseSources();
            else if (!_paused) ResumeMusicOnly();
        }

        public void StopPendingGameplayAudio()
        {
            _scheduled.Clear();
            if (_voices == null) return;
            for (int i = 0; i < _voices.Length; i++) _voices[i].Stop();
            _duckUntil = 0f;
            ApplyVolumes();
        }

        private void ScheduleWave(SessionActionResult action, int waveOrdinal, int waveIndex, ref float delay)
        {
            int groups = GroupCount(action, waveOrdinal);
            int audibleGroups = Mathf.Clamp(groups, 1, 3);
            for (int group = 0; group < audibleGroups; group++)
            {
                Schedule(Pick(settings.Pops), settings.PopVolume, RandomPitch(), delay + (group * 0.025f));
            }
            _haptics.Play(HapticCue.Match);

            if (waveIndex >= 1)
            {
                int cascadeIndex = Mathf.Clamp(waveIndex - 1, 0, settings.Cascades.Length - 1);
                float pitch = 1f + Mathf.Min(waveIndex - 1, 5) * 0.025f;
                if (waveIndex > 4) pitch += RandomRange(-0.012f, 0.012f);
                Schedule(settings.Cascades[cascadeIndex], settings.CascadeVolume, pitch, delay + 0.035f,
                    waveIndex >= 2);
            }
            delay += WaveInterval;
        }

        private static int GroupCount(SessionActionResult action, int waveOrdinal)
        {
            int primaryWaves = action.Resolution == null ? 0 : action.Resolution.Waves.Count;
            if (waveOrdinal < primaryWaves)
                return action.Resolution.Waves[waveOrdinal].GroupSizes.Count;
            int pressureIndex = waveOrdinal - primaryWaves;
            return action.PressureResolution != null && pressureIndex < action.PressureResolution.Waves.Count
                ? action.PressureResolution.Waves[pressureIndex].GroupSizes.Count
                : 0;
        }

        private static bool FindCue(SessionActionResult action, PresentationCue cue)
        {
            for (int i = 0; i < action.Events.Count; i++)
                if (action.Events[i].Cue == cue) return true;
            return false;
        }

        public void PlayDailyComplete()
        {
            if (settings != null) Schedule(settings.DailyComplete, settings.ResultVolume, 1f, 0.32f);
        }

        private void PlayVariant(AudioClip[] variants, float volume)
        {
            Play(Pick(variants), volume, RandomPitch());
        }

        private AudioClip Pick(AudioClip[] variants)
        {
            if (variants == null || variants.Length == 0) return null;
            return variants[_cosmeticRandom.Next(variants.Length)];
        }

        private float RandomPitch() => 1f + RandomRange(-settings.VariantPitchRange, settings.VariantPitchRange);
        private float RandomRange(float min, float max) => min + ((float)_cosmeticRandom.NextDouble() * (max - min));

        private void Schedule(AudioClip clip, float volume, float pitch, float delay, bool major = false)
        {
            if (clip == null || _muted || _paused || _interrupted) return;
            _scheduled.Add(new ScheduledSound
            {
                At = Time.unscaledTime + Mathf.Max(0f, delay),
                Clip = clip,
                Volume = volume,
                Pitch = pitch,
                Major = major
            });
        }

        private void Play(AudioClip clip, float volume, float pitch, bool major = false)
        {
            if (clip == null || _muted || _paused || _interrupted || _voices == null) return;
            AudioSource source = FindVoice();
            source.Stop();
            source.clip = clip;
            source.pitch = Mathf.Clamp(pitch, 0.85f, 1.18f);
            float duck = !major && Time.unscaledTime < _duckUntil ? 0.58f : 1f;
            source.volume = Mathf.Clamp01(volume * _sfxVolume * _masterVolume * duck);
            source.Play();
            IssuedSoundCount++;
            if (major) _duckUntil = Mathf.Max(_duckUntil, Time.unscaledTime + 0.38f);
        }

#if UNITY_EDITOR
        public void PlayPreview(AudioClip clip, float volume = 0.7f, float pitch = 1f)
        {
            Play(clip, volume, pitch);
        }
#endif

        private AudioSource FindVoice()
        {
            for (int offset = 0; offset < _voices.Length; offset++)
            {
                int index = (_nextVoice + offset) % _voices.Length;
                if (_voices[index].isPlaying) continue;
                _nextVoice = (index + 1) % _voices.Length;
                return _voices[index];
            }
            AudioSource reused = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;
            return reused;
        }

        private void BuildPool()
        {
            _voices = new AudioSource[settings.VoiceLimit];
            for (int i = 0; i < _voices.Length; i++)
            {
                var voiceObject = new GameObject($"SFX Voice {i + 1}");
                voiceObject.transform.SetParent(transform, false);
                _voices[i] = ConfigureSource(voiceObject.AddComponent<AudioSource>());
            }
            var musicObject = new GameObject("Music Voice");
            musicObject.transform.SetParent(transform, false);
            _musicSource = ConfigureSource(musicObject.AddComponent<AudioSource>());
            _musicSource.loop = true;
        }

        private static AudioSource ConfigureSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            return source;
        }

        private void PauseSources()
        {
            if (_voices != null) for (int i = 0; i < _voices.Length; i++) _voices[i].Pause();
            _musicSource?.Pause();
        }

        private void ResumeMusicOnly()
        {
            if (_musicSource != null && _musicSource.clip != null) _musicSource.UnPause();
        }

        private void SaveAndApplyVolumes()
        {
            PlayerPrefs.Save();
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            if (_musicSource == null) return;
            float duck = Time.unscaledTime < _duckUntil ? 0.42f : 1f;
            _musicSource.volume = _muted ? 0f : _masterVolume * _musicVolume * duck;
        }
    }
}
