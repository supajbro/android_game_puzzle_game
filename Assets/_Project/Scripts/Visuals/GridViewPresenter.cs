using System;
using System.Collections.Generic;
using JellyRiot.Core;
using UnityEngine;
using UnityEngine.UI;

namespace JellyRiot.Visuals
{
    /// <summary>
    /// Replays semantic Core results as presentation only, then reconciles every pooled visual to
    /// the authoritative snapshot. Animation state never feeds back into gameplay.
    /// </summary>
    public sealed class GridViewPresenter : MonoBehaviour
    {
        private enum DropPhase { None, Falling, Landing, Rebound, Settling }

        private sealed class PresentationRequest
        {
            public SessionActionResult Begin;
            public SessionActionResult Complete;
            public SessionActionResult Terminal;
            public Action Finished;
            public bool AnimateDrop;
        }

        private sealed class JellyVisual
        {
            private readonly Image _image;
            private readonly Image _shadow;
            private readonly Vector2 _imagePosition;
            private readonly Vector2 _shadowPosition;
            private readonly Vector2 _imageSize;
            private readonly Vector2 _shadowSize;
            private readonly Sprite _shadowSprite;
            private readonly Color _shadowColour;

            public JellyVisual(Image image, Image shadow)
            {
                _image = image ?? throw new ArgumentNullException(nameof(image));
                _shadow = shadow;
                _imagePosition = image.rectTransform.anchoredPosition;
                _imageSize = image.rectTransform.sizeDelta;
                _shadowPosition = shadow == null ? Vector2.zero : shadow.rectTransform.anchoredPosition;
                _shadowSize = shadow == null ? Vector2.zero : shadow.rectTransform.sizeDelta;
                _shadowSprite = shadow == null ? null : shadow.sprite;
                _shadowColour = shadow == null ? Color.white : shadow.color;
            }

            public Image Image => _image;
            public Vector2 RestPosition => _imagePosition;
            public Vector2 Size => _imageSize;

            public void Show(Sprite sprite)
            {
                ResetTransform(_image, _imagePosition, _imageSize, Color.white);
                _image.sprite = sprite;
                _image.gameObject.SetActive(true);
                if (_shadow == null) return;
                ResetTransform(_shadow, _shadowPosition, _shadowSize, _shadowColour);
                _shadow.sprite = _shadowSprite;
                _shadow.gameObject.SetActive(true);
            }

            public void SetFallingPose(Sprite sprite, float y, float scaleX, float scaleY,
                float shadowScale, float shadowAlpha)
            {
                _image.sprite = sprite;
                _image.rectTransform.anchoredPosition = new Vector2(_imagePosition.x, y);
                _image.rectTransform.localScale = new Vector3(scaleX, scaleY, 1f);
                if (_shadow == null) return;
                _shadow.rectTransform.localScale = new Vector3(shadowScale, shadowScale, 1f);
                SetShadowAlpha(shadowAlpha);
            }

            public void SetBottomAnchoredPose(Sprite sprite, float scaleX, float scaleY,
                float bounceHeight, float shadowScale, float shadowAlpha)
            {
                _image.sprite = sprite;
                float anchoredOffset = (_imageSize.y * (scaleY - 1f) * 0.5f) + bounceHeight;
                _image.rectTransform.anchoredPosition = _imagePosition + new Vector2(0f, anchoredOffset);
                _image.rectTransform.localScale = new Vector3(scaleX, scaleY, 1f);
                if (_shadow == null) return;
                _shadow.rectTransform.localScale = new Vector3(shadowScale, shadowScale, 1f);
                SetShadowAlpha(shadowAlpha);
            }

            public void SetSprite(Sprite sprite) => _image.sprite = sprite;

            public void ReturnToPool()
            {
                ResetTransform(_image, _imagePosition, _imageSize, Color.white);
                _image.sprite = null;
                _image.gameObject.SetActive(false);
                if (_shadow == null) return;
                ResetTransform(_shadow, _shadowPosition, _shadowSize, _shadowColour);
                _shadow.sprite = _shadowSprite;
                _shadow.gameObject.SetActive(false);
            }

            public bool IsAtRest() =>
                _image.rectTransform.anchoredPosition == _imagePosition &&
                _image.rectTransform.localRotation == Quaternion.identity &&
                _image.rectTransform.localScale == Vector3.one &&
                _image.rectTransform.sizeDelta == _imageSize;

            private void SetShadowAlpha(float multiplier)
            {
                Color colour = _shadowColour;
                colour.a *= Mathf.Clamp01(multiplier);
                _shadow.color = colour;
                _shadow.canvasRenderer.SetAlpha(1f);
            }

            private static void ResetTransform(Image image, Vector2 position, Vector2 size, Color colour)
            {
                image.rectTransform.anchoredPosition = position;
                image.rectTransform.sizeDelta = size;
                image.rectTransform.localRotation = Quaternion.identity;
                image.rectTransform.localScale = Vector3.one;
                image.color = colour;
                image.canvasRenderer.SetAlpha(1f);
            }
        }

        private static readonly Color ValidColumn = new Color32(69, 223, 245, 105);
        private static readonly Color InvalidColumn = new Color32(255, 102, 141, 170);

        [Header("Drop timing")]
        [SerializeField, Min(0.01f)] private float minimumFallDuration = 0.14f;
        [SerializeField, Min(0.01f)] private float maximumFallDuration = 0.24f;
        [SerializeField, Min(0.01f)] private float reducedMotionFallDuration = 0.08f;
        [SerializeField, Min(0.01f)] private float landingDuration = 0.07f;
        [SerializeField, Min(0.01f)] private float reboundDuration = 0.085f;
        [SerializeField, Min(0.01f)] private float settlingDuration = 0.07f;
        [SerializeField, Min(0f)] private float primaryBounceHeight = 10f;
        [SerializeField, Range(0.05f, 1f)] private float slowMotionMultiplier = 0.25f;

        [Header("Idle blink")]
        [SerializeField, Min(0.1f)] private float blinkDuration = 0.28f;
        [SerializeField, Min(0.1f)] private float minimumBlinkInterval = 1.8f;
        [SerializeField, Min(0.1f)] private float maximumBlinkInterval = 4.5f;

        private readonly Queue<PresentationRequest> _eventQueue = new Queue<PresentationRequest>(4);
        private readonly JellyColour[] _renderedColours = new JellyColour[BoardModel.CellCount];
        private JellyVisual[] _board;
        private JellyVisual[] _rack;
        private Image[] _rackSelections;
        private Image[] _columnFeedback;
        private Button[] _columnButtons;
        private Sprite[] _staticSprites;
        private JellyAnimationLibrary _animationLibrary;
        private GameSession _session;
        private Text _debugText;
        private PresentationRequest _activeRequest;
        private JellyVisual _activeDrop;
        private Sprite[] _fallingFrames;
        private Sprite[] _landingFrames;
        private Sprite[] _squashFrames;
        private JellyColour _dropColour;
        private DropPhase _dropPhase;
        private float _phaseElapsed;
        private float _fallDuration;
        private float _spawnY;
        private int _lastProcessedSequence;
        private int _invalidColumn = -1;
        private float _invalidUntil;
        private bool _reducedMotion;
        private bool _slowMotion;
        private System.Random _cosmeticRandom;
        private float _presentationClock;
        private float _nextBlinkAt;
        private int _blinkCell = -1;
        private int _lastBlinkCell = -1;
        private float _blinkElapsed;
        private Sprite[] _blinkFrames;

        public int QueuedActionCount => _eventQueue.Count + (_activeRequest == null ? 0 : 1);
        public int LastProcessedSequence => _lastProcessedSequence;
        public bool IsPresenting => _activeRequest != null;
        public bool ReducedMotion => _reducedMotion;
        public bool SlowMotionEnabled => _slowMotion;
        public float ActiveFallDuration => _fallDuration;

        public void Initialise(GameSession session, Sprite[] staticSprites,
            JellyAnimationLibrary animationLibrary, Image[] boardJellies, Image[] boardShadows,
            Image[] rackJellies, Image[] rackShadows, Image[] rackSelections,
            Image[] columnFeedback, Button[] columnButtons, Text debugText = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (staticSprites == null || staticSprites.Length != 5)
                throw new ArgumentException("Exactly five static jelly sprites are required.", nameof(staticSprites));
            ValidateLength(boardJellies, BoardModel.CellCount, nameof(boardJellies));
            ValidateLength(boardShadows, BoardModel.CellCount, nameof(boardShadows));
            ValidateLength(rackJellies, GameSession.RackSize, nameof(rackJellies));
            ValidateLength(rackShadows, GameSession.RackSize, nameof(rackShadows));
            ValidateLength(rackSelections, GameSession.RackSize, nameof(rackSelections));
            ValidateLength(columnFeedback, BoardModel.Width, nameof(columnFeedback));
            ValidateLength(columnButtons, BoardModel.Width, nameof(columnButtons));

            _session = session;
            _staticSprites = (Sprite[])staticSprites.Clone();
            _animationLibrary = animationLibrary;
            _rackSelections = rackSelections;
            _columnFeedback = columnFeedback;
            _columnButtons = columnButtons;
            _debugText = debugText;
            _board = new JellyVisual[BoardModel.CellCount];
            _rack = new JellyVisual[GameSession.RackSize];
            for (int i = 0; i < _board.Length; i++) _board[i] = new JellyVisual(boardJellies[i], boardShadows[i]);
            for (int i = 0; i < _rack.Length; i++) _rack[i] = new JellyVisual(rackJellies[i], rackShadows[i]);
            _cosmeticRandom = new System.Random(unchecked(Environment.TickCount ^ GetInstanceID()));
            ScheduleNextBlink();
            CancelPendingAndSynchronise();
        }

        public void Present(SessionActionResult action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            CancelBlink();
            _eventQueue.Enqueue(new PresentationRequest { Begin = action });
            PumpQueue();
        }

        public void PresentTurn(SessionActionResult begin, SessionActionResult complete,
            SessionActionResult terminal = null, Action finished = null)
        {
            if (begin == null) throw new ArgumentNullException(nameof(begin));
            if (complete == null) throw new ArgumentNullException(nameof(complete));
            CancelBlink();
            _eventQueue.Enqueue(new PresentationRequest
            {
                Begin = begin,
                Complete = complete,
                Terminal = terminal,
                Finished = finished,
                AnimateDrop = begin.Succeeded
            });
            PumpQueue();
        }

        public void SetReducedMotion(bool reduced)
        {
            _reducedMotion = reduced;
            if (reduced) CancelBlink();
        }

        public void ToggleSlowMotion()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _slowMotion = !_slowMotion;
#else
            _slowMotion = false;
#endif
        }

        public bool HasCompleteAnimationLibrary()
        {
            if (_animationLibrary == null) return false;
            try
            {
                for (JellyColour colour = JellyColour.Coral; colour <= JellyColour.Mint; colour++)
                for (JellyAnimationType type = JellyAnimationType.Falling; type <= JellyAnimationType.Blink; type++)
                {
                    Sprite[] frames = _animationLibrary.GetFrames(colour, type);
                    if (frames == null || frames.Length != 5) return false;
                    for (int i = 0; i < frames.Length; i++) if (frames[i] == null) return false;
                }
                return true;
            }
            catch (InvalidOperationException) { return false; }
        }

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
            _dropPhase = DropPhase.None;
            _fallingFrames = null;
            _landingFrames = null;
            _squashFrames = null;
            _invalidColumn = -1;
            CancelBlink();
            SynchroniseSnapshot();
            finished?.Invoke();
        }

        public bool RenderedBoardMatchesModel()
        {
            if (_session?.Board == null || _board == null || IsPresenting) return false;
            for (int i = 0; i < _renderedColours.Length; i++)
            {
                JellyColour model = _session.Board.Cells[i];
                if (_renderedColours[i] != model || _board[i].Image.gameObject.activeSelf != (model != JellyColour.Empty)) return false;
                if (model != JellyColour.Empty && (_board[i].Image.sprite != SpriteFor(model) || !_board[i].IsAtRest())) return false;
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
            _presentationClock += deltaTime;
            if (_dropPhase != DropPhase.None) AdvanceDrop(deltaTime);
            else AdvanceBlink(deltaTime);
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
            _dropColour = placement.Colour;
            _activeDrop.Show(SpriteFor(_dropColour));
            _renderedColours[index] = _dropColour;
            _spawnY = _board[0].RestPosition.y + (_activeDrop.Size.y * 1.35f);
            _fallDuration = _reducedMotion ? reducedMotionFallDuration :
                Mathf.Lerp(minimumFallDuration, maximumFallDuration,
                    coordinate.Row / (float)(BoardModel.Height - 1));
            _fallingFrames = FramesOrNull(_dropColour, JellyAnimationType.Falling);
            _landingFrames = FramesOrNull(_dropColour, JellyAnimationType.Landing);
            _squashFrames = FramesOrNull(_dropColour, JellyAnimationType.SquashStretch);
            _phaseElapsed = 0f;
            _dropPhase = DropPhase.Falling;
            HideConsumedRackSlot(request.Begin);
            RefreshColumnFeedback();
            ApplyFallPose(0f);
            UpdateDiagnostics();
            return true;
        }

        private void AdvanceDrop(float deltaTime)
        {
            _phaseElapsed += deltaTime;
            switch (_dropPhase)
            {
                case DropPhase.Falling:
                {
                    float progress = Mathf.Clamp01(_phaseElapsed / _fallDuration);
                    ApplyFallPose(progress);
                    if (progress >= 1f)
                    {
                        if (_reducedMotion) FinishDrop();
                        else BeginPhase(DropPhase.Landing);
                    }
                    break;
                }
                case DropPhase.Landing:
                {
                    float progress = Mathf.Clamp01(_phaseElapsed / landingDuration);
                    float squash = Mathf.Sin(progress * Mathf.PI);
                    _activeDrop.SetBottomAnchoredPose(FrameAt(_landingFrames, progress, _dropColour),
                        1f + (0.045f * squash), 1f - (0.04f * squash), 0f,
                        1f + (0.09f * squash), 1f);
                    if (progress >= 1f) BeginPhase(DropPhase.Rebound);
                    break;
                }
                case DropPhase.Rebound:
                {
                    float progress = Mathf.Clamp01(_phaseElapsed / reboundDuration);
                    float arc = Mathf.Sin(progress * Mathf.PI);
                    _activeDrop.SetBottomAnchoredPose(FrameAt(_squashFrames, progress * 0.6f, _dropColour),
                        1f - (0.018f * arc), 1f + (0.025f * arc), primaryBounceHeight * arc,
                        1f - (0.045f * arc), 1f - (0.12f * arc));
                    if (progress >= 1f) BeginPhase(DropPhase.Settling);
                    break;
                }
                case DropPhase.Settling:
                {
                    float progress = Mathf.Clamp01(_phaseElapsed / settlingDuration);
                    float arc = Mathf.Sin(progress * Mathf.PI);
                    _activeDrop.SetBottomAnchoredPose(FrameAt(_squashFrames, 0.6f + (progress * 0.4f), _dropColour),
                        1f + (0.01f * arc), 1f - (0.012f * arc), primaryBounceHeight * 0.28f * arc,
                        1f + (0.02f * arc), 1f);
                    if (progress >= 1f) FinishDrop();
                    break;
                }
            }
        }

        private void ApplyFallPose(float progress)
        {
            float accelerated = progress * progress;
            float y = Mathf.LerpUnclamped(_spawnY, _activeDrop.RestPosition.y, accelerated);
            Sprite sprite = _reducedMotion ? SpriteFor(_dropColour) : FrameAt(_fallingFrames, progress, _dropColour);
            float stretch = _reducedMotion ? 0f : Mathf.Sin(progress * Mathf.PI) * 0.025f;
            _activeDrop.SetFallingPose(sprite, y, 1f - stretch, 1f + stretch,
                Mathf.Lerp(0.72f, 1f, accelerated), Mathf.Lerp(0.5f, 1f, accelerated));
        }

        private void BeginPhase(DropPhase phase)
        {
            _dropPhase = phase;
            _phaseElapsed = 0f;
        }

        private void FinishDrop()
        {
            _activeDrop.Show(SpriteFor(_dropColour));
            _dropPhase = DropPhase.None;
            PresentationRequest completed = _activeRequest;
            _activeRequest = null;
            _activeDrop = null;
            _fallingFrames = null;
            _landingFrames = null;
            _squashFrames = null;
            RecordAndReplay(completed.Begin);
            RecordAndReplay(completed.Complete);
            RecordAndReplay(completed.Terminal);
            SynchroniseSnapshot();
            ScheduleNextBlink();
            completed.Finished?.Invoke();
            PumpQueue();
        }

        private void ProcessRequestImmediately(PresentationRequest request)
        {
            RecordAndReplay(request.Begin);
            RecordAndReplay(request.Complete);
            RecordAndReplay(request.Terminal);
            SynchroniseSnapshot();
            request.Finished?.Invoke();
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
                if (sessionEvent.Type == SessionEventType.JellyPlaced && sessionEvent.Coordinate.HasValue)
                {
                    BoardCoordinate coordinate = sessionEvent.Coordinate.Value;
                    SetRenderedCell((coordinate.Row * BoardModel.Width) + coordinate.Column, sessionEvent.Colour);
                }
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

        private void AdvanceBlink(float deltaTime)
        {
            if (_reducedMotion || _session.State != SessionState.Ready || _activeRequest != null) return;
            if (_blinkCell < 0)
            {
                if (_presentationClock >= _nextBlinkAt) TryStartBlink();
                return;
            }
            _blinkElapsed += deltaTime;
            float progress = Mathf.Clamp01(_blinkElapsed / blinkDuration);
            _board[_blinkCell].SetSprite(FrameAt(_blinkFrames, progress, _renderedColours[_blinkCell]));
            if (progress < 1f) return;
            _board[_blinkCell].SetSprite(SpriteFor(_renderedColours[_blinkCell]));
            _lastBlinkCell = _blinkCell;
            _blinkCell = -1;
            _blinkFrames = null;
            ScheduleNextBlink();
        }

        private void TryStartBlink()
        {
            int start = _cosmeticRandom.Next(BoardModel.CellCount);
            for (int offset = 0; offset < BoardModel.CellCount; offset++)
            {
                int index = (start + offset) % BoardModel.CellCount;
                if (index == _lastBlinkCell || _renderedColours[index] == JellyColour.Empty) continue;
                Sprite[] frames = FramesOrNull(_renderedColours[index], JellyAnimationType.Blink);
                if (frames == null) break;
                _blinkCell = index;
                _blinkElapsed = 0f;
                _blinkFrames = frames;
                return;
            }
            ScheduleNextBlink();
        }

        private void CancelBlink()
        {
            if (_blinkCell >= 0 && _board != null)
            {
                JellyColour colour = _renderedColours[_blinkCell];
                if (colour != JellyColour.Empty) _board[_blinkCell].SetSprite(SpriteFor(colour));
            }
            _blinkCell = -1;
            _blinkFrames = null;
            _blinkElapsed = 0f;
            ScheduleNextBlink();
        }

        private void ScheduleNextBlink()
        {
            if (_cosmeticRandom == null) return;
            float interval = minimumBlinkInterval +
                ((float)_cosmeticRandom.NextDouble() * (maximumBlinkInterval - minimumBlinkInterval));
            _nextBlinkAt = _presentationClock + interval;
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
                else _rack[slot].Show(SpriteFor(colour));
                _rackSelections[slot].gameObject.SetActive(_session.SelectedSlot == slot);
            }
            RefreshColumnFeedback();
        }

        private void SetRenderedCell(int index, JellyColour colour)
        {
            _renderedColours[index] = colour;
            if (colour == JellyColour.Empty) _board[index].ReturnToPool();
            else _board[index].Show(SpriteFor(colour));
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
                if (_board[i].Image.gameObject.activeSelf) active++;
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

        private Sprite[] FramesOrNull(JellyColour colour, JellyAnimationType type)
        {
            if (_animationLibrary == null) return null;
            try
            {
                Sprite[] frames = _animationLibrary.GetFrames(colour, type);
                if (frames == null || frames.Length != 5) return null;
                for (int i = 0; i < frames.Length; i++) if (frames[i] == null) return null;
                return frames;
            }
            catch (InvalidOperationException) { return null; }
        }

        private Sprite FrameAt(Sprite[] frames, float progress, JellyColour fallbackColour)
        {
            if (frames == null || frames.Length == 0) return SpriteFor(fallbackColour);
            int index = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(Mathf.Clamp01(progress) * frames.Length));
            return frames[index] == null ? SpriteFor(fallbackColour) : frames[index];
        }

        private float VisualPlaybackScale()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            return _slowMotion ? slowMotionMultiplier : 1f;
#else
            return 1f;
#endif
        }

        private Sprite SpriteFor(JellyColour colour)
        {
            int index = (int)colour - 1;
            return index >= 0 && index < _staticSprites.Length ? _staticSprites[index] : null;
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
