using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JellyRiot.Core
{
    /// <summary>Authoritative rack, input gate, turn state and score orchestration.</summary>
    public sealed class GameSession
    {
        public const int RackSize = 3;

        private readonly GameBalance _balance;
        private readonly JellyColour[] _rack = new JellyColour[RackSize];
        private readonly ReadOnlyCollection<JellyColour> _readOnlyRack;
        private int _eventSequence;
        private int _consumedSlot = -1;

        public GameSession() : this(GameBalance.Default)
        {
        }

        public GameSession(GameBalance balance)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _readOnlyRack = Array.AsReadOnly(_rack);
            Score = new ScoreTracker();
            State = SessionState.Boot;
            SelectedSlot = -1;
        }

        public SessionState State { get; private set; }
        public BoardModel Board { get; private set; }
        public IReadOnlyList<JellyColour> Rack => _readOnlyRack;
        public int SelectedSlot { get; private set; }
        public ulong Seed { get; private set; }
        public GameMode Mode { get; private set; }
        public string DailyKey { get; private set; }
        public ScoreTracker Score { get; }
        public RunScoreResult CurrentResult { get; private set; }

        public SessionActionResult CompleteBoot()
        {
            var events = new List<SessionEvent>(1);
            if (State != SessionState.Boot)
            {
                return Reject(SessionFailure.InvalidState, events);
            }

            TransitionTo(SessionState.Home, events);
            return Success(events);
        }

        public SessionActionResult StartNewRun(ulong seed, GameMode mode = GameMode.Endless,
            string dailyKey = null)
        {
            var events = new List<SessionEvent>(5);
            if (State != SessionState.Home && State != SessionState.GameOver)
            {
                return Reject(SessionFailure.InvalidState, events);
            }

            return StartRun(seed, mode, dailyKey, events);
        }

        public SessionActionResult Replay()
        {
            var events = new List<SessionEvent>(5);
            if (State != SessionState.GameOver)
            {
                return Reject(SessionFailure.InvalidState, events);
            }

            return StartRun(Seed, Mode, DailyKey, events);
        }

        public SessionActionResult ResetRun()
        {
            var events = new List<SessionEvent>(5);
            if (State == SessionState.Boot || State == SessionState.Home || State == SessionState.Resolving)
            {
                return Reject(SessionFailure.InvalidState, events);
            }

            return StartRun(Seed, Mode, DailyKey, events);
        }

        public SessionActionResult ReturnHome()
        {
            var events = new List<SessionEvent>(1);
            if (State == SessionState.Boot || State == SessionState.Home || State == SessionState.Resolving)
            {
                return Reject(SessionFailure.InvalidState, events);
            }

            SelectedSlot = -1;
            TransitionTo(SessionState.Home, events);
            return Success(events);
        }

        public SessionActionResult Pause()
        {
            var events = new List<SessionEvent>(1);
            if (State != SessionState.Ready)
            {
                return Reject(SessionFailure.InvalidState, events);
            }

            TransitionTo(SessionState.Paused, events);
            return Success(events);
        }

        public SessionActionResult Resume()
        {
            var events = new List<SessionEvent>(1);
            if (State != SessionState.Paused)
            {
                return Reject(SessionFailure.InvalidState, events);
            }

            TransitionTo(SessionState.Ready, events);
            return Success(events);
        }

        /// <summary>Future pressure logic calls this without mutating the settled board.</summary>
        public SessionActionResult EnterPendingLoss()
        {
            var events = new List<SessionEvent>(1);
            if (State != SessionState.Ready)
            {
                return Reject(SessionFailure.InvalidState, events);
            }

            TransitionTo(SessionState.PendingLoss, events);
            return Success(events);
        }

        public SessionActionResult ConfirmGameOver()
        {
            var events = new List<SessionEvent>(4);
            if (State != SessionState.Ready && State != SessionState.PendingLoss)
            {
                return Reject(SessionFailure.InvalidState, events);
            }

            CurrentResult = Score.FinaliseRun();
            events.Add(NewEvent(SessionEventType.RunFinalised, score: CurrentResult.FinalScore));
            for (int i = 0; i < CurrentResult.Presentation.Count; i++)
            {
                PresentationMetadata metadata = CurrentResult.Presentation[i];
                events.Add(NewEvent(SessionEventType.PresentationCue, waveIndex: metadata.WaveIndex,
                    score: metadata.Score, cue: metadata.Cue));
            }

            TransitionTo(SessionState.GameOver, events);
            return Success(events);
        }

        public SessionActionResult SelectRackSlot(int slotIndex)
        {
            var events = new List<SessionEvent>(1);
            if (State != SessionState.Ready)
            {
                return Reject(SessionFailure.InvalidState, events);
            }

            if (slotIndex < 0 || slotIndex >= RackSize)
            {
                return Reject(SessionFailure.InvalidSlot, events);
            }

            SelectedSlot = slotIndex;
            events.Add(NewEvent(SessionEventType.RackSlotSelected, slotIndex: slotIndex,
                colour: _rack[slotIndex]));
            return Success(events);
        }

        /// <summary>Accepts at most one placement and leaves the session observably Resolving.</summary>
        public SessionActionResult TryBeginTurn(int column)
        {
            var events = new List<SessionEvent>(3);
            if (State != SessionState.Ready)
            {
                return Reject(SessionFailure.InvalidState, events);
            }

            if (SelectedSlot < 0)
            {
                return Reject(SessionFailure.NoSelection, events);
            }

            if (column < 0 || column >= BoardModel.Width)
            {
                return Reject(SessionFailure.InvalidColumn, events, column);
            }

            if (Board.IsColumnFull(column))
            {
                return Reject(SessionFailure.ColumnFull, events, column);
            }

            int slot = SelectedSlot;
            JellyColour colour = _rack[slot];
            MoveResult move = Board.PlaceJelly(column, colour);
            if (!move.Succeeded)
            {
                SessionFailure failure = move.Failure == PlacementFailure.ColumnFull
                    ? SessionFailure.ColumnFull
                    : SessionFailure.InvalidColumn;
                return Reject(failure, events, column);
            }

            _rack[slot] = JellyColour.Empty;
            _consumedSlot = slot;
            SelectedSlot = -1;
            events.Add(NewEvent(SessionEventType.JellyPlaced, slotIndex: slot, column: column,
                coordinate: move.PlacedAt, colour: colour));
            events.Add(NewEvent(SessionEventType.RackSlotConsumed, slotIndex: slot, colour: colour));
            TransitionTo(SessionState.Resolving, events);
            return Success(events);
        }

        public SessionActionResult CompleteTurn()
        {
            var events = new List<SessionEvent>(12);
            if (State != SessionState.Resolving || _consumedSlot < 0)
            {
                return Reject(SessionFailure.InvalidState, events);
            }

            BoardResolutionResult resolution = Board.ResolveMatches();
            ScoreResolutionResult scoring = Score.ApplyResolution(resolution);
            for (int i = 0; i < resolution.Waves.Count; i++)
            {
                ResolutionWave wave = resolution.Waves[i];
                WaveScore waveScore = scoring.Waves[i];
                events.Add(NewEvent(SessionEventType.WaveResolved, waveIndex: wave.Index,
                    clearedCount: wave.ClearedCount, points: waveScore.Points,
                    score: waveScore.ScoreAfterWave));
                events.Add(NewEvent(SessionEventType.ScoreChanged, waveIndex: wave.Index,
                    points: waveScore.Points, score: waveScore.ScoreAfterWave));

                for (int cueIndex = 0; cueIndex < scoring.Presentation.Count; cueIndex++)
                {
                    PresentationMetadata metadata = scoring.Presentation[cueIndex];
                    if (metadata.WaveIndex == wave.Index)
                    {
                        events.Add(NewEvent(SessionEventType.PresentationCue,
                            waveIndex: metadata.WaveIndex, score: metadata.Score, cue: metadata.Cue));
                    }
                }
            }

            int refilledSlot = _consumedSlot;
            JellyColour refilledColour = Board.NextGeneratedColour();
            _rack[refilledSlot] = refilledColour;
            _consumedSlot = -1;
            events.Add(NewEvent(SessionEventType.RackSlotRefilled, slotIndex: refilledSlot,
                colour: refilledColour));
            events.Add(NewEvent(SessionEventType.TurnCompleted, score: Score.CurrentScore));
            TransitionTo(SessionState.Ready, events);
            return new SessionActionResult(true, SessionFailure.None, events, resolution, scoring);
        }

        private SessionActionResult StartRun(ulong seed, GameMode mode, string dailyKey,
            List<SessionEvent> events)
        {
            SessionState previousState = State;
            Score.StartRun(mode, dailyKey);
            Board = new BoardModel(seed, _balance);
            Seed = seed;
            Mode = mode;
            DailyKey = mode == GameMode.Daily ? dailyKey : null;
            CurrentResult = null;
            SelectedSlot = -1;
            _consumedSlot = -1;
            _eventSequence = 0;

            for (int i = 0; i < RackSize; i++)
            {
                _rack[i] = Board.NextGeneratedColour();
            }

            events.Add(NewEvent(SessionEventType.RunStarted));
            for (int i = 0; i < RackSize; i++)
            {
                events.Add(NewEvent(SessionEventType.RackSlotInitialised, slotIndex: i,
                    colour: _rack[i]));
            }

            State = SessionState.Ready;
            events.Add(NewEvent(SessionEventType.StateChanged, previousState: previousState));
            return Success(events);
        }

        private void TransitionTo(SessionState next, List<SessionEvent> events)
        {
            SessionState previous = State;
            State = next;
            events.Add(NewEvent(SessionEventType.StateChanged, previousState: previous));
        }

        private SessionEvent NewEvent(SessionEventType type, SessionState? state = null,
            SessionState previousState = SessionState.Boot, SessionFailure failure = SessionFailure.None,
            int slotIndex = -1, int column = -1, BoardCoordinate? coordinate = null,
            JellyColour colour = JellyColour.Empty, int waveIndex = -1, int clearedCount = 0,
            long points = 0L, long score = 0L, PresentationCue? cue = null)
        {
            return new SessionEvent(++_eventSequence, type, state ?? State, previousState, failure,
                slotIndex, column, coordinate, colour, waveIndex, clearedCount, points, score, cue);
        }

        private SessionActionResult Reject(SessionFailure failure, List<SessionEvent> events,
            int column = -1)
        {
            events.Add(NewEvent(SessionEventType.ActionRejected, failure: failure,
                slotIndex: SelectedSlot, column: column,
                colour: SelectedSlot >= 0 ? _rack[SelectedSlot] : JellyColour.Empty));
            return new SessionActionResult(false, failure, events);
        }

        private static SessionActionResult Success(List<SessionEvent> events) =>
            new SessionActionResult(true, SessionFailure.None, events);
    }
}
