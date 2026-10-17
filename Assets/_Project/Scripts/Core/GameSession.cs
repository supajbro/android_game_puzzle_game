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
        private RunContentGenerator _generator;
        private int _nextPressureTurn;
        private int _lastPressureTurn;
        private SessionSnapshot _continueCheckpoint;

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
        public int NextRowIn => Board == null ? 0 : Math.Max(0, _nextPressureTurn - Board.TurnCount);
        public SessionSnapshot ContinueCheckpoint => _continueCheckpoint;

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

            FinaliseRun(events);
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
            AppendResolutionEvents(resolution, scoring, events);

            int refilledSlot = _consumedSlot;
            Board.UpdateActiveColourCount(_balance.GetActiveColourCount(Board.TurnCount));
            JellyColour refilledColour = _generator.NextColour(
                Board.ActiveColourCount, _balance.UseWeightedGeneration);
            _rack[refilledSlot] = refilledColour;
            _consumedSlot = -1;
            events.Add(NewEvent(SessionEventType.RackSlotRefilled, slotIndex: refilledSlot,
                colour: refilledColour));

            JellyColour[] pressureRow = null;
            BoardResolutionResult pressureResolution = null;
            ScoreResolutionResult pressureScoring = null;
            if (Board.TurnCount >= _nextPressureTurn)
            {
                _continueCheckpoint = CreateSnapshotInternal(SessionState.Ready, false);
                events.Add(NewEvent(SessionEventType.CheckpointCaptured));
                if (!Board.CanApplyPressureRow())
                {
                    events.Add(NewEvent(SessionEventType.PressurePending));
                    TransitionTo(SessionState.PendingLoss, events);
                    return new SessionActionResult(true, SessionFailure.None, events, resolution, scoring);
                }

                pressureRow = _generator.NextPressureRow(_balance, Board.ActiveColourCount);
                Board.ApplyPressureRow(pressureRow);
                _lastPressureTurn = Board.TurnCount;
                _nextPressureTurn = _lastPressureTurn + _balance.GetPressureIntervalAfter(_lastPressureTurn);
                events.Add(NewEvent(SessionEventType.PressureRowApplied));
                pressureResolution = Board.ResolveMatches();
                pressureScoring = Score.ApplyResolution(pressureResolution);
                AppendResolutionEvents(pressureResolution, pressureScoring, events);
            }

            if (!Board.HasLegalColumn())
            {
                events.Add(NewEvent(SessionEventType.NoLegalMove));
                FinaliseRun(events);
                return new SessionActionResult(true, SessionFailure.None, events, resolution, scoring,
                    pressureRow, pressureResolution, pressureScoring);
            }

            events.Add(NewEvent(SessionEventType.TurnCompleted, score: Score.CurrentScore));
            TransitionTo(SessionState.Ready, events);
            return new SessionActionResult(true, SessionFailure.None, events, resolution, scoring,
                pressureRow, pressureResolution, pressureScoring);
        }

        private SessionActionResult StartRun(ulong seed, GameMode mode, string dailyKey,
            List<SessionEvent> events)
        {
            SessionState previousState = State;
            Score.StartRun(mode, dailyKey);
            Board = new BoardModel(seed, _balance);
            _generator = new RunContentGenerator(seed);
            Seed = seed;
            Mode = mode;
            DailyKey = mode == GameMode.Daily ? dailyKey : null;
            CurrentResult = null;
            SelectedSlot = -1;
            _consumedSlot = -1;
            _eventSequence = 0;
            _lastPressureTurn = 0;
            _nextPressureTurn = _balance.FirstPressureTurn;
            _continueCheckpoint = null;

            for (int i = 0; i < RackSize; i++)
            {
                _rack[i] = _generator.NextColour(
                    Board.ActiveColourCount, _balance.UseWeightedGeneration);
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

        public SessionSnapshot CreateSnapshot() => CreateSnapshotInternal(null, true);

        public void Restore(SessionSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Board == null || snapshot.Rack == null ||
                snapshot.Rack.Length != RackSize || snapshot.Generator == null || snapshot.Score == null ||
                snapshot.SelectedSlot < -1 || snapshot.SelectedSlot >= RackSize ||
                snapshot.ConsumedSlot < -1 || snapshot.ConsumedSlot >= RackSize ||
                snapshot.NextPressureTurn < 1 || snapshot.LastPressureTurn < 0)
            {
                throw new ArgumentException("Session snapshot is invalid.", nameof(snapshot));
            }

            var board = new BoardModel(snapshot.Seed, _balance);
            board.Restore(snapshot.Board);
            var generator = new RunContentGenerator(snapshot.Seed);
            generator.Restore(snapshot.Generator);
            Score.Restore(snapshot.Score);

            Board = board;
            _generator = generator;
            Array.Copy(snapshot.Rack, _rack, RackSize);
            Seed = snapshot.Seed;
            Mode = snapshot.Mode;
            DailyKey = snapshot.DailyKey;
            State = snapshot.State;
            SelectedSlot = snapshot.SelectedSlot;
            _consumedSlot = snapshot.ConsumedSlot;
            _nextPressureTurn = snapshot.NextPressureTurn;
            _lastPressureTurn = snapshot.LastPressureTurn;
            _eventSequence = snapshot.EventSequence;
            _continueCheckpoint = snapshot.ContinueCheckpoint;
            CurrentResult = snapshot.HasCurrentResult
                ? new RunScoreResult(snapshot.Mode, snapshot.DailyKey, snapshot.ResultFinalScore,
                    snapshot.ResultLongestChain, snapshot.ResultPersonalBest,
                    snapshot.ResultPersonalBest
                        ? new[] { new PresentationMetadata(PresentationCue.PersonalBest, -1,
                            snapshot.ResultFinalScore) }
                        : Array.Empty<PresentationMetadata>())
                : null;
        }

        private SessionSnapshot CreateSnapshotInternal(SessionState? stateOverride, bool includeCheckpoint)
        {
            if (Board == null || _generator == null)
            {
                throw new InvalidOperationException("A run must exist before it can be snapshotted.");
            }

            return new SessionSnapshot
            {
                Seed = Seed,
                Mode = Mode,
                DailyKey = DailyKey,
                State = stateOverride ?? State,
                Board = Board.CreateSnapshot(),
                Rack = (JellyColour[])_rack.Clone(),
                SelectedSlot = SelectedSlot,
                ConsumedSlot = _consumedSlot,
                Generator = _generator.CreateSnapshot(),
                Score = Score.CreateSnapshot(),
                NextPressureTurn = _nextPressureTurn,
                LastPressureTurn = _lastPressureTurn,
                EventSequence = _eventSequence,
                HasCurrentResult = CurrentResult != null,
                ResultFinalScore = CurrentResult?.FinalScore ?? 0L,
                ResultLongestChain = CurrentResult?.LongestChain ?? 0,
                ResultPersonalBest = CurrentResult?.IsPersonalBest ?? false,
                ContinueCheckpoint = includeCheckpoint ? _continueCheckpoint : null
            };
        }

        private void AppendResolutionEvents(BoardResolutionResult resolution,
            ScoreResolutionResult scoring, List<SessionEvent> events)
        {
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
        }

        private void FinaliseRun(List<SessionEvent> events)
        {
            CurrentResult = Score.FinaliseRun();
            events.Add(NewEvent(SessionEventType.RunFinalised, score: CurrentResult.FinalScore));
            for (int i = 0; i < CurrentResult.Presentation.Count; i++)
            {
                PresentationMetadata metadata = CurrentResult.Presentation[i];
                events.Add(NewEvent(SessionEventType.PresentationCue, waveIndex: metadata.WaveIndex,
                    score: metadata.Score, cue: metadata.Cue));
            }

            TransitionTo(SessionState.GameOver, events);
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
