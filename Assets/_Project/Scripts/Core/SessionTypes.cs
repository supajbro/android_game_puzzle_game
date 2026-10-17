using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JellyRiot.Core
{
    public enum SessionState
    {
        Boot = 0,
        Home = 1,
        Ready = 2,
        Resolving = 3,
        PendingLoss = 4,
        GameOver = 5,
        Paused = 6
    }

    public enum SessionFailure
    {
        None = 0,
        InvalidState = 1,
        InvalidSlot = 2,
        NoSelection = 3,
        InvalidColumn = 4,
        ColumnFull = 5
    }

    public enum SessionEventType
    {
        StateChanged = 0,
        RunStarted = 1,
        RackSlotInitialised = 2,
        RackSlotSelected = 3,
        ActionRejected = 4,
        JellyPlaced = 5,
        RackSlotConsumed = 6,
        WaveResolved = 7,
        ScoreChanged = 8,
        PresentationCue = 9,
        RackSlotRefilled = 10,
        TurnCompleted = 11,
        RunFinalised = 12,
        PressurePending = 13,
        PressureRowApplied = 14,
        NoLegalMove = 15,
        CheckpointCaptured = 16
    }

    [Serializable]
    public sealed class SessionEvent
    {
        internal SessionEvent(int sequence, SessionEventType type, SessionState state,
            SessionState previousState = SessionState.Boot, SessionFailure failure = SessionFailure.None,
            int slotIndex = -1, int column = -1, BoardCoordinate? coordinate = null,
            JellyColour colour = JellyColour.Empty, int waveIndex = -1, int clearedCount = 0,
            long points = 0L, long score = 0L, PresentationCue? cue = null)
        {
            Sequence = sequence;
            Type = type;
            State = state;
            PreviousState = previousState;
            Failure = failure;
            SlotIndex = slotIndex;
            Column = column;
            Coordinate = coordinate;
            Colour = colour;
            WaveIndex = waveIndex;
            ClearedCount = clearedCount;
            Points = points;
            Score = score;
            Cue = cue;
        }

        public int Sequence { get; }
        public SessionEventType Type { get; }
        public SessionState State { get; }
        public SessionState PreviousState { get; }
        public SessionFailure Failure { get; }
        public int SlotIndex { get; }
        public int Column { get; }
        public BoardCoordinate? Coordinate { get; }
        public JellyColour Colour { get; }
        public int WaveIndex { get; }
        public int ClearedCount { get; }
        public long Points { get; }
        public long Score { get; }
        public PresentationCue? Cue { get; }
    }

    [Serializable]
    public sealed class SessionActionResult
    {
        internal SessionActionResult(bool succeeded, SessionFailure failure, IList<SessionEvent> events,
            BoardResolutionResult resolution = null, ScoreResolutionResult scoring = null,
            JellyColour[] pressureRow = null, BoardResolutionResult pressureResolution = null,
            ScoreResolutionResult pressureScoring = null)
        {
            Succeeded = succeeded;
            Failure = failure;
            Events = new ReadOnlyCollection<SessionEvent>(new List<SessionEvent>(events));
            Resolution = resolution;
            Scoring = scoring;
            PressureRow = pressureRow == null
                ? null
                : new ReadOnlyCollection<JellyColour>((JellyColour[])pressureRow.Clone());
            PressureResolution = pressureResolution;
            PressureScoring = pressureScoring;
        }

        public bool Succeeded { get; }
        public SessionFailure Failure { get; }
        public IReadOnlyList<SessionEvent> Events { get; }
        public BoardResolutionResult Resolution { get; }
        public ScoreResolutionResult Scoring { get; }
        public IReadOnlyList<JellyColour> PressureRow { get; }
        public BoardResolutionResult PressureResolution { get; }
        public ScoreResolutionResult PressureScoring { get; }
    }
}
