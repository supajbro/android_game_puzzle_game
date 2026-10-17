using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JellyRiot.Core
{
    public enum GameMode
    {
        Endless = 0,
        Daily = 1
    }

    public enum PresentationCue
    {
        Chain2 = 0,
        Riot3Plus = 1,
        LargeGroup8Plus = 2,
        PersonalBest = 3
    }

    [Serializable]
    public sealed class ScoreTrackerSnapshot
    {
        public long CurrentScore;
        public int LongestChain;
        public long BestEndlessScore;
        public GameMode Mode;
        public string DailyKey;
        public bool RunActive;
        public string[] DailyKeys;
        public long[] DailyScores;
    }

    [Serializable]
    public readonly struct PresentationMetadata : IEquatable<PresentationMetadata>
    {
        public PresentationMetadata(PresentationCue cue, int waveIndex, long score)
        {
            Cue = cue;
            WaveIndex = waveIndex;
            Score = score;
        }

        public PresentationCue Cue { get; }
        public int WaveIndex { get; }
        public long Score { get; }

        public bool Equals(PresentationMetadata other) =>
            Cue == other.Cue && WaveIndex == other.WaveIndex && Score == other.Score;

        public override bool Equals(object obj) => obj is PresentationMetadata other && Equals(other);

        public override int GetHashCode() =>
            unchecked((((int)Cue * 397) ^ WaveIndex) * 397 ^ Score.GetHashCode());
    }

    [Serializable]
    public readonly struct WaveScore
    {
        public WaveScore(int waveIndex, int clearedCount, long points, long scoreAfterWave)
        {
            WaveIndex = waveIndex;
            ClearedCount = clearedCount;
            Points = points;
            ScoreAfterWave = scoreAfterWave;
        }

        public int WaveIndex { get; }
        public int ClearedCount { get; }
        public long Points { get; }
        public long ScoreAfterWave { get; }
    }

    [Serializable]
    public sealed class ScoreResolutionResult
    {
        internal ScoreResolutionResult(IList<WaveScore> waves, IList<PresentationMetadata> presentation)
        {
            Waves = new ReadOnlyCollection<WaveScore>(new List<WaveScore>(waves));
            Presentation = new ReadOnlyCollection<PresentationMetadata>(
                new List<PresentationMetadata>(presentation));
        }

        public IReadOnlyList<WaveScore> Waves { get; }
        public IReadOnlyList<PresentationMetadata> Presentation { get; }
    }

    [Serializable]
    public sealed class RunScoreResult
    {
        internal RunScoreResult(GameMode mode, string dailyKey, long finalScore, int longestChain,
            bool isPersonalBest, IList<PresentationMetadata> presentation)
        {
            Mode = mode;
            DailyKey = dailyKey;
            FinalScore = finalScore;
            LongestChain = longestChain;
            IsPersonalBest = isPersonalBest;
            Presentation = new ReadOnlyCollection<PresentationMetadata>(
                new List<PresentationMetadata>(presentation));
        }

        public GameMode Mode { get; }
        public string DailyKey { get; }
        public long FinalScore { get; }
        public int LongestChain { get; }
        public bool IsPersonalBest { get; }
        public IReadOnlyList<PresentationMetadata> Presentation { get; }
    }

    public static class ScoreCalculator
    {
        public static long CalculateWavePoints(int clearedCellCount, int oneBasedWaveIndex)
        {
            if (clearedCellCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(clearedCellCount));
            }

            if (oneBasedWaveIndex <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(oneBasedWaveIndex));
            }

            long cells = clearedCellCount;
            long multiplier = 10L * oneBasedWaveIndex;
            if (cells != 0L && multiplier > long.MaxValue / cells)
            {
                return long.MaxValue;
            }

            return cells * multiplier;
        }

        internal static long SaturatingAdd(long left, long right)
        {
            if (right > 0L && left > long.MaxValue - right)
            {
                return long.MaxValue;
            }

            return left + right;
        }
    }

    /// <summary>Mode-aware deterministic score records. Presentation only observes its outputs.</summary>
    public sealed class ScoreTracker
    {
        private readonly Dictionary<string, long> _dailyBestScores =
            new Dictionary<string, long>(StringComparer.Ordinal);
        private GameMode _mode;
        private string _dailyKey;
        private bool _runActive;

        public long CurrentScore { get; private set; }
        public int LongestChain { get; private set; }
        public long BestEndlessScore { get; private set; }
        public GameMode Mode => _mode;
        public string DailyKey => _dailyKey;

        public void StartRun(GameMode mode, string dailyKey = null)
        {
            if (mode != GameMode.Endless && mode != GameMode.Daily)
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            if (mode == GameMode.Daily && string.IsNullOrWhiteSpace(dailyKey))
            {
                throw new ArgumentException("Daily runs require a stable day key.", nameof(dailyKey));
            }

            _mode = mode;
            _dailyKey = mode == GameMode.Daily ? dailyKey : null;
            CurrentScore = 0L;
            LongestChain = 0;
            _runActive = true;
        }

        public ScoreResolutionResult ApplyResolution(BoardResolutionResult resolution)
        {
            if (resolution == null)
            {
                throw new ArgumentNullException(nameof(resolution));
            }

            if (!_runActive)
            {
                throw new InvalidOperationException("A run must be started before score can be applied.");
            }

            var scores = new List<WaveScore>(resolution.WaveCount);
            var presentation = new List<PresentationMetadata>(3);
            for (int i = 0; i < resolution.Waves.Count; i++)
            {
                ResolutionWave wave = resolution.Waves[i];
                int chainNumber = wave.Index + 1;
                long points = ScoreCalculator.CalculateWavePoints(wave.ClearedCount, chainNumber);
                CurrentScore = ScoreCalculator.SaturatingAdd(CurrentScore, points);
                scores.Add(new WaveScore(wave.Index, wave.ClearedCount, points, CurrentScore));

                if (chainNumber == 2)
                {
                    presentation.Add(new PresentationMetadata(PresentationCue.Chain2, wave.Index, CurrentScore));
                }

                if (chainNumber == 3)
                {
                    presentation.Add(new PresentationMetadata(PresentationCue.Riot3Plus, wave.Index, CurrentScore));
                }

                if (wave.LargestGroupSize >= 8)
                {
                    presentation.Add(new PresentationMetadata(
                        PresentationCue.LargeGroup8Plus, wave.Index, CurrentScore));
                }
            }

            if (resolution.WaveCount > LongestChain)
            {
                LongestChain = resolution.WaveCount;
            }

            return new ScoreResolutionResult(scores, presentation);
        }

        public RunScoreResult FinaliseRun()
        {
            if (!_runActive)
            {
                throw new InvalidOperationException("There is no active run to finalise.");
            }

            bool personalBest;
            if (_mode == GameMode.Endless)
            {
                personalBest = CurrentScore > BestEndlessScore;
                if (personalBest)
                {
                    BestEndlessScore = CurrentScore;
                }
            }
            else
            {
                long previous = GetDailyBest(_dailyKey);
                personalBest = CurrentScore > previous;
                if (personalBest)
                {
                    _dailyBestScores[_dailyKey] = CurrentScore;
                }
            }

            var presentation = new List<PresentationMetadata>(personalBest ? 1 : 0);
            if (personalBest)
            {
                presentation.Add(new PresentationMetadata(
                    PresentationCue.PersonalBest, -1, CurrentScore));
            }

            _runActive = false;
            return new RunScoreResult(_mode, _dailyKey, CurrentScore, LongestChain,
                personalBest, presentation);
        }

        public long GetDailyBest(string dailyKey)
        {
            if (string.IsNullOrWhiteSpace(dailyKey))
            {
                throw new ArgumentException("A daily key is required.", nameof(dailyKey));
            }

            return _dailyBestScores.TryGetValue(dailyKey, out long score) ? score : 0L;
        }

        public ScoreTrackerSnapshot CreateSnapshot()
        {
            var keys = new string[_dailyBestScores.Count];
            var scores = new long[_dailyBestScores.Count];
            int index = 0;
            foreach (KeyValuePair<string, long> pair in _dailyBestScores)
            {
                keys[index] = pair.Key;
                scores[index] = pair.Value;
                index++;
            }

            Array.Sort(keys, scores, StringComparer.Ordinal);
            return new ScoreTrackerSnapshot
            {
                CurrentScore = CurrentScore,
                LongestChain = LongestChain,
                BestEndlessScore = BestEndlessScore,
                Mode = _mode,
                DailyKey = _dailyKey,
                RunActive = _runActive,
                DailyKeys = keys,
                DailyScores = scores
            };
        }

        public void Restore(ScoreTrackerSnapshot snapshot)
        {
            if (snapshot == null || snapshot.CurrentScore < 0L || snapshot.LongestChain < 0 ||
                snapshot.BestEndlessScore < 0L || snapshot.DailyKeys == null ||
                snapshot.DailyScores == null || snapshot.DailyKeys.Length != snapshot.DailyScores.Length ||
                (snapshot.Mode != GameMode.Endless && snapshot.Mode != GameMode.Daily) ||
                (snapshot.Mode == GameMode.Daily && string.IsNullOrWhiteSpace(snapshot.DailyKey)))
            {
                throw new ArgumentException("Score snapshot is invalid.", nameof(snapshot));
            }

            _dailyBestScores.Clear();
            for (int i = 0; i < snapshot.DailyKeys.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(snapshot.DailyKeys[i]) || snapshot.DailyScores[i] < 0L)
                {
                    throw new ArgumentException("Score snapshot contains an invalid daily record.", nameof(snapshot));
                }

                _dailyBestScores.Add(snapshot.DailyKeys[i], snapshot.DailyScores[i]);
            }

            CurrentScore = snapshot.CurrentScore;
            LongestChain = snapshot.LongestChain;
            BestEndlessScore = snapshot.BestEndlessScore;
            _mode = snapshot.Mode;
            _dailyKey = snapshot.DailyKey;
            _runActive = snapshot.RunActive;
        }
    }
}
