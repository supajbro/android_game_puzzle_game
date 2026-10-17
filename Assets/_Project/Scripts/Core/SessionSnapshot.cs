using System;

namespace JellyRiot.Core
{
    /// <summary>Complete deterministic session state for save/load and replay verification.</summary>
    [Serializable]
    public sealed class SessionSnapshot
    {
        public ulong Seed;
        public GameMode Mode;
        public string DailyKey;
        public SessionState State;
        public RunSnapshot Board;
        public JellyColour[] Rack;
        public int SelectedSlot;
        public int ConsumedSlot;
        public ContentGeneratorSnapshot Generator;
        public ScoreTrackerSnapshot Score;
        public int NextPressureTurn;
        public int LastPressureTurn;
        public int EventSequence;
        public bool HasCurrentResult;
        public long ResultFinalScore;
        public int ResultLongestChain;
        public bool ResultPersonalBest;
        public SessionSnapshot ContinueCheckpoint;
    }
}
