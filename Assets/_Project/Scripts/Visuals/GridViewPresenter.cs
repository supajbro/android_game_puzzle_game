using System;
using System.Collections.Generic;
using JellyRiot.Core;
using UnityEngine;
using UnityEngine.UI;

namespace JellyRiot.Visuals
{
    /// <summary>Replays Core events through animated views, then reconciles to the authoritative snapshot.</summary>
    public sealed class GridViewPresenter : MonoBehaviour
    {
        private sealed class PresentationRequest
        {
            public SessionActionResult Begin;
            public SessionActionResult Complete;
            public SessionActionResult Terminal;
            public Action Finished;
            public bool AnimateDrop;
        }

        private static readonly Color ValidColumn = new Color32(69, 223, 245, 105);
        private static readonly Color InvalidColumn = new Color32(255, 102, 141, 170);

        [Header("Drop timing")]
        [SerializeField, Min(0.01f)] private float minimumFallDuration = 0.14f;
        [SerializeField, Min(0.01f)] private float maximumFallDuration = 0.24f;
        [SerializeField, Min(0.01f)] private float reducedMotionFallDuration = 0.08f;
        [SerializeField, Range(0.05f, 1f)] private float slowMotionMultiplier = 0.25f;

        private readonly Queue<PresentationRequest> _eventQueue = new Queue<PresentationRequest>(4);
        private readonly JellyColour[] _renderedColours = new JellyColour[BoardModel.CellCount];
        private AnimatedJellyView[] _board;
        private AnimatedJellyView[] _rack;
        private Image[] _rackSelections;
        private Image[] _columnFeedback;
        private Button[] _columnButtons;
        private JellyAnimationLibrary _animationLibrary;
        private GameSession _session;
        private Text _debugText;
        private PresentationRequest _activeRequest;
        private AnimatedJellyView _activeDrop;
        private int _lastProcessedSequence;
        private int _invalidColumn = -1;
        private float _invalidUntil;
        private bool _reducedMotion;
        private bool _slowMotion;
        private float _fallDuration;
        private int _activePlacementSequence;

        public event Action<int> JellyLanded;
        public event Action TurnPresentationCompleted;

        public int QueuedActionCount => _eventQueue.Count + (_activeRequest == null ? 0 : 1);
        public int LastProcessedSequence => _lastProcessedSequence;
        public bool IsPresenting => _activeRequest != null;
        public bool ReducedMotion => _reducedMotion;
        public bool SlowMotionEnabled => _slowMotion;
        public float ActiveFallDuration => _fallDuration;

        public void Initialise(GameSession session, JellyAnimationLibrary animationLibrary,
            Image[] boardJellies, Image[] boardShadows, Image[] rackJellies, Image[] rackShadows,
            Image[] rackSelections, Image[] columnFeedback, Button[] columnButtons, Text debugText = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (animationLibrary == null || !animationLibrary.IsComplete())
                throw new ArgumentException("A complete expressive jelly animation library is required.", nameof(animationLibrary));
            ValidateLength(boardJellies, BoardModel.CellCount, nameof(boardJellies));
            ValidateLength(boardShadows, BoardModel.CellCount, nameof(boardShadows));
            ValidateLength(rackJellies, GameSession.RackSize, nameof(rackJellies));
            ValidateLength(rackShadows, GameSession.RackSize, nameof(rackShadows));
            ValidateLength(rackSelections, GameSession.RackSize, nameof(rackSelections));
            ValidateLength(columnFeedback, BoardModel.Width, nameof(columnFeedback));
            ValidateLength(columnButtons, BoardModel.Width, nameof(columnButtons));

            _session = session;
            _animationLibrary = animationLibrary;
            _rackSelections = rackSelections;
            _columnFeedback = columnFeedback;
            _columnButtons = columnButtons;
            _debugText = debugText;
            _board = new AnimatedJellyView[BoardModel.CellCount];
            _rack = new AnimatedJellyView[GameSession.RackSize];
            int seedBase = unchecked(Environment.TickCount ^ GetInstanceID());
            for (int i = 0; i < _board.Length; i++)
            {
                _board[i] = boardJellies[i].transform.parent.gameObject.AddComponent<AnimatedJellyView>();
                _board[i].Initialise(boardJellies[i], boardShadows[i], animationLibrary,
                    unchecked(seedBase + (i * 7919)));
            }
            for (int i = 0; i < _rack.Length; i++)
            {
                _rack[i] = rackJellies[i].transform.parent.gameObject.AddComponent<AnimatedJellyView>();
                _rack[i].Initialise(rackJellies[i], rackShadows[i], animationLibrary,
                    unchecked(seedBase + 104729 + (i * 7919)));
            }
            CancelPendingAndSynchronise();
        }

        public void Present(SessionActionResult action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            _eventQueue.Enqueue(new PresentationRequest { Begin = action });
            PumpQueue();
        }

        public void PresentTurn(SessionActionResult begin, SessionActionResult complete,
            SessionActionResult terminal = null, Action finished = null)
        {
            if (begin == null) throw new ArgumentNullException(nameof(begin));
            if (complete == null) throw new ArgumentNullException(nameof(complete));
            _eventQueue.Enqueue(new PresentationRequest
            {
                Begin = begin, Complete = complete, Terminal = terminal, Finished = finished,
                AnimateDrop = begin.Succeeded
            });
            PumpQueue();
        }

        public void SetReducedMotion(bool reduced)
        {
            _reducedMotion = reduced;
            if (_board != null) for (int i = 0; i < _board.Length; i++) _board[i].SetReducedMotion(reduced);
            if (_rack != null) for (int i = 0; i < _rack.Length; i++) _rack[i].SetReducedMotion(reduced);
        }

        public void ToggleSlowMotion()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _slowMotion = !_slowMotion;
#else
            _slowMotion = false;
#endif
        }

        public bool HasCompleteAnimationLibrary() => _animationLibrary != null && _animationLibrary.IsComplete();

        public void SynchroniseSnapshot()
        {
            if (_session == null || _session.Board == null || _board == null) return;
            RunSnapshot snapshot = _session.Board.CreateSnapshot();
            for (int i = 0; i < snapshot.Cells.Length; i++) SetRenderedCell(i, snapshot.Cells[i]);
            SynchroniseRackAndControls();
            UpdateDiagnostics();
        }

        public void ShowInvalidColumn(int column)
        {
            if (_columnFeedback == null || column < 0 || column >= _columnFeedback.Length) return;
            _invalidColumn = column;
            _invalidUntil = Time.unscaledTime + 0.18f;
            _columnFeedback[column].color = InvalidColumn;
            _columnFeedback[column].gameObject.SetActive(true);
        }

        public void CancelPendingAndSynchronise()
        {
            Action finished = _activeRequest?.Finished;
            _eventQueue.Clear();
            _activeRequest = null;
            _activeDrop = null;
            _invalidColumn = -1;
            SynchroniseSnapshot();
            finished?.Invoke();
        }

        public bool RenderedBoardMatchesModel()
        {
            if (_session?.Board == null || _board == null || IsPresenting) return false;
            for (int i = 0; i < _renderedColours.Length; i++)
            {
                JellyColour model = _session.Board.Cells[i];
                if (_renderedColours[i] != model || _board[i].IsVisible != (model != JellyColour.Empty)) return false;
                if (model != JellyColour.Empty && _board[i].Colour != model) return false;
            }
            return true;
        }

        private void Update()
        {
            if (_invalidColumn >= 0 && Time.unscaledTime >= _invalidUntil)
            {
                _invalidColumn = -1;
                RefreshColumnFeedback();
            }
            if (_session == null || _session.State == SessionState.Paused) return;
            float deltaTime = Time.deltaTime * VisualPlaybackScale();
            if (deltaTime <= 0f) return;
            for (int i = 0; i < _board.Length; i++) _board[i].Tick(deltaTime);
            for (int i = 0; i < _rack.Length; i++) _rack[i].Tick(deltaTime);
            if (_activeDrop == null) return;
            if (_activeDrop.ConsumeLandingImpact()) JellyLanded?.Invoke(_activePlacementSequence);
            if (!_activeDrop.IsDropPlaying) FinishDrop();
        }

        private void PumpQueue()
        {
            if (_activeRequest != null) return;
            while (_eventQueue.Count > 0 && _activeRequest == null)
            {
                PresentationRequest request = _eventQueue.Dequeue();
                if (request.AnimateDrop && TryBeginDrop(request)) return;
                ProcessRequestImmediately(request);
            }
        }

        private bool TryBeginDrop(PresentationRequest request)
        {
            if (!TryFindPlacement(request.Begin, out SessionEvent placement)) return false;
            BoardCoordinate coordinate = placement.Coordinate.Value;
            int index = (coordinate.Row * BoardModel.Width) + coordinate.Column;
            _activeRequest = request;
            _activeDrop = _board[index];
            _activePlacementSequence = placement.Sequence;
            _renderedColours[index] = placement.Colour;
            float spawnY = _board[0].RootPosition.y - _activeDrop.RootPosition.y +
                (_activeDrop.Size.y * 1.35f);
            _fallDuration = _reducedMotion ? reducedMotionFallDuration :
                Mathf.Lerp(minimumFallDuration, maximumFallDuration, coordinate.Row / (float)(BoardModel.Height - 1));
            _activeDrop.PlayDrop(placement.Colour, spawnY, _fallDuration, _reducedMotion);
            HideConsumedRackSlot(request.Begin);
            RefreshColumnFeedback();
            UpdateDiagnostics();
            return true;
        }

        private void FinishDrop()
        {
            PresentationRequest completed = _activeRequest;
            _activeRequest = null;
            _activeDrop = null;
            RecordAndReplay(completed.Begin);
            RecordAndReplay(completed.Complete);
            RecordAndReplay(completed.Terminal);
            SynchroniseSnapshot();
            completed.Finished?.Invoke();
            TurnPresentationCompleted?.Invoke();
            PumpQueue();
        }

        private void ProcessRequestImmediately(PresentationRequest request)
        {
            RecordAndReplay(request.Begin);
            RecordAndReplay(request.Complete);
            RecordAndReplay(request.Terminal);
            SynchroniseSnapshot();
            request.Finished?.Invoke();
            if (request.AnimateDrop) TurnPresentationCompleted?.Invoke();
        }

        private void RecordAndReplay(SessionActionResult action)
        {
            if (action == null) return;
            for (int i = 0; i < action.Events.Count; i++)
            {
                SessionEvent sessionEvent = action.Events[i];
                if (sessionEvent.Type == SessionEventType.RunStarted) _lastProcessedSequence = 0;
                if (sessionEvent.Sequence > _lastProcessedSequence) _lastProcessedSequence = sessionEvent.Sequence;
            }
            ReplaySemanticChanges(action);
        }

        private void ReplaySemanticChanges(SessionActionResult action)
        {
            for (int i = 0; i < action.Events.Count; i++)
            {
                SessionEvent sessionEvent = action.Events[i];
                if (sessionEvent.Type != SessionEventType.JellyPlaced || !sessionEvent.Coordinate.HasValue) continue;
                BoardCoordinate coordinate = sessionEvent.Coordinate.Value;
                SetRenderedCell((coordinate.Row * BoardModel.Width) + coordinate.Column, sessionEvent.Colour);
            }
            ReplayResolution(action.Resolution);
            if (action.PressureRow != null)
            {
                for (int row = 0; row < BoardModel.Height - 1; row++)
                for (int column = 0; column < BoardModel.Width; column++)
                    SetRenderedCell((row * BoardModel.Width) + column,
                        _renderedColours[((row + 1) * BoardModel.Width) + column]);
                for (int column = 0; column < BoardModel.Width; column++)
                    SetRenderedCell(((BoardModel.Height - 1) * BoardModel.Width) + column, action.PressureRow[column]);
            }
            ReplayResolution(action.PressureResolution);
        }

        private void ReplayResolution(BoardResolutionResult resolution)
        {
            if (resolution == null) return;
            for (int waveIndex = 0; waveIndex < resolution.Waves.Count; waveIndex++)
            {
                ResolutionWave wave = resolution.Waves[waveIndex];
                for (int i = 0; i < wave.Cleared.Count; i++)
                {
                    BoardCoordinate position = wave.Cleared[i].Position;
                    SetRenderedCell((position.Row * BoardModel.Width) + position.Column, JellyColour.Empty);
                }
                for (int i = 0; i < wave.Falls.Count; i++)
                {
                    FallingJelly fall = wave.Falls[i];
                    SetRenderedCell((fall.From.Row * BoardModel.Width) + fall.From.Column, JellyColour.Empty);
                }
                for (int i = 0; i < wave.Falls.Count; i++)
                {
                    FallingJelly fall = wave.Falls[i];
                    SetRenderedCell((fall.To.Row * BoardModel.Width) + fall.To.Column, fall.Colour);
                }
            }
        }

        private void HideConsumedRackSlot(SessionActionResult begin)
        {
            for (int i = 0; i < begin.Events.Count; i++)
            {
                SessionEvent sessionEvent = begin.Events[i];
                if (sessionEvent.Type != SessionEventType.RackSlotConsumed || sessionEvent.SlotIndex < 0) continue;
                _rack[sessionEvent.SlotIndex].ReturnToPool();
                _rackSelections[sessionEvent.SlotIndex].gameObject.SetActive(false);
                break;
            }
        }

        private void SynchroniseRackAndControls()
        {
            for (int slot = 0; slot < GameSession.RackSize; slot++)
            {
                JellyColour colour = _session.Rack[slot];
                if (colour == JellyColour.Empty) _rack[slot].ReturnToPool();
                else _rack[slot].SetColour(colour);
                _rackSelections[slot].gameObject.SetActive(_session.SelectedSlot == slot);
            }
            RefreshColumnFeedback();
        }

        private void SetRenderedCell(int index, JellyColour colour)
        {
            _renderedColours[index] = colour;
            if (colour == JellyColour.Empty) _board[index].ReturnToPool();
            else _board[index].SetColour(colour);
        }

        private void RefreshColumnFeedback()
        {
            if (_session?.Board == null) return;
            bool canDrop = !IsPresenting && _session.State == SessionState.Ready && _session.SelectedSlot >= 0;
            for (int column = 0; column < BoardModel.Width; column++)
            {
                bool legal = _session.State == SessionState.Ready && !_session.Board.IsColumnFull(column);
                _columnButtons[column].interactable = !IsPresenting && _session.State == SessionState.Ready;
                if (column == _invalidColumn && Time.unscaledTime < _invalidUntil) continue;
                _columnFeedback[column].color = ValidColumn;
                _columnFeedback[column].gameObject.SetActive(canDrop && legal);
            }
        }

        private void UpdateDiagnostics()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (_debugText == null) return;
            int active = 0;
            int duplicateReferences = 0;
            var seen = new HashSet<Image>();
            for (int i = 0; i < _renderedColours.Length; i++)
            {
                if (_board[i].IsVisible) active++;
                if (!seen.Add(_board[i].Image)) duplicateReferences++;
            }
            string sync = IsPresenting ? "ANIMATING" : RenderedBoardMatchesModel() ? "OK" : "MISMATCH";
            _debugText.text = $"VISUAL SYNC: {sync}\nACTIVE: {active}  DUPLICATES: {duplicateReferences}\n" +
                $"QUEUED: {QueuedActionCount}  EVENT: {_lastProcessedSequence}";
            _debugText.gameObject.SetActive(true);
#else
            if (_debugText != null) _debugText.gameObject.SetActive(false);
#endif
        }

        private float VisualPlaybackScale()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            return _slowMotion ? slowMotionMultiplier : 1f;
#else
            return 1f;
#endif
        }

        private static bool TryFindPlacement(SessionActionResult action, out SessionEvent placement)
        {
            for (int i = 0; i < action.Events.Count; i++)
            {
                if (action.Events[i].Type == SessionEventType.JellyPlaced && action.Events[i].Coordinate.HasValue)
                {
                    placement = action.Events[i];
                    return true;
                }
            }
            placement = null;
            return false;
        }

        private static void ValidateLength(Array values, int length, string parameterName)
        {
            if (values == null || values.Length != length)
                throw new ArgumentException($"Expected exactly {length} entries.", parameterName);
        }
    }
}
