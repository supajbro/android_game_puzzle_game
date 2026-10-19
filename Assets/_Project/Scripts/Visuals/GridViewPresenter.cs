using System;
using System.Collections.Generic;
using JellyRiot.Core;
using UnityEngine;
using UnityEngine.UI;

namespace JellyRiot.Visuals
{
    /// <summary>
    /// Motion-safe binding between Core state and pooled UI visuals. Events may drive temporary
    /// presentation later; every action currently ends by reconciling against the authoritative snapshot.
    /// </summary>
    public sealed class GridViewPresenter : MonoBehaviour
    {
        private sealed class JellyVisual
        {
            private readonly Image _image;
            private readonly Image _shadow;
            private readonly Vector2 _imagePosition;
            private readonly Vector2 _shadowPosition;
            private readonly Vector2 _imageSize;
            private readonly Vector2 _shadowSize;
            private readonly Sprite _shadowSprite;

            public JellyVisual(Image image, Image shadow)
            {
                _image = image ?? throw new ArgumentNullException(nameof(image));
                _shadow = shadow;
                _imagePosition = image.rectTransform.anchoredPosition;
                _imageSize = image.rectTransform.sizeDelta;
                _shadowPosition = shadow == null ? Vector2.zero : shadow.rectTransform.anchoredPosition;
                _shadowSize = shadow == null ? Vector2.zero : shadow.rectTransform.sizeDelta;
                _shadowSprite = shadow == null ? null : shadow.sprite;
            }

            public Image Image => _image;

            public void Show(Sprite sprite)
            {
                ResetTransform(_image, _imagePosition, _imageSize);
                _image.sprite = sprite;
                _image.gameObject.SetActive(true);
                if (_shadow != null)
                {
                    ResetTransform(_shadow, _shadowPosition, _shadowSize);
                    _shadow.sprite = _shadowSprite;
                    _shadow.gameObject.SetActive(true);
                }
            }

            public void ReturnToPool()
            {
                ResetTransform(_image, _imagePosition, _imageSize);
                _image.sprite = null;
                _image.gameObject.SetActive(false);
                if (_shadow != null)
                {
                    ResetTransform(_shadow, _shadowPosition, _shadowSize);
                    _shadow.sprite = _shadowSprite;
                    _shadow.gameObject.SetActive(false);
                }
            }

            public bool IsAtRest()
            {
                return _image.rectTransform.anchoredPosition == _imagePosition &&
                       _image.rectTransform.localRotation == Quaternion.identity &&
                       _image.rectTransform.localScale == Vector3.one &&
                       _image.rectTransform.sizeDelta == _imageSize;
            }

            private static void ResetTransform(Image image, Vector2 position, Vector2 size)
            {
                image.rectTransform.anchoredPosition = position;
                image.rectTransform.sizeDelta = size;
                image.rectTransform.localRotation = Quaternion.identity;
                image.rectTransform.localScale = Vector3.one;
                image.color = Color.white;
                image.canvasRenderer.SetAlpha(1f);
            }
        }

        private static readonly Color ValidColumn = new Color32(69, 223, 245, 105);
        private static readonly Color InvalidColumn = new Color32(255, 102, 141, 170);

        private readonly Queue<SessionActionResult> _eventQueue = new Queue<SessionActionResult>(4);
        private readonly JellyColour[] _renderedColours = new JellyColour[BoardModel.CellCount];
        private JellyVisual[] _board;
        private JellyVisual[] _rack;
        private Image[] _rackSelections;
        private Image[] _columnFeedback;
        private Button[] _columnButtons;
        private Sprite[] _staticSprites;
        private GameSession _session;
        private Text _debugText;
        private bool _processing;
        private int _lastProcessedSequence;
        private int _invalidColumn = -1;
        private float _invalidUntil;

        public int QueuedActionCount => _eventQueue.Count;
        public int LastProcessedSequence => _lastProcessedSequence;

        public void Initialise(GameSession session, Sprite[] staticSprites, Image[] boardJellies,
            Image[] boardShadows, Image[] rackJellies, Image[] rackShadows, Image[] rackSelections,
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
            _rackSelections = rackSelections;
            _columnFeedback = columnFeedback;
            _columnButtons = columnButtons;
            _debugText = debugText;
            _board = new JellyVisual[BoardModel.CellCount];
            _rack = new JellyVisual[GameSession.RackSize];
            for (int i = 0; i < _board.Length; i++) _board[i] = new JellyVisual(boardJellies[i], boardShadows[i]);
            for (int i = 0; i < _rack.Length; i++) _rack[i] = new JellyVisual(rackJellies[i], rackShadows[i]);
            CancelPendingAndSynchronise();
        }

        public void Present(SessionActionResult action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            _eventQueue.Enqueue(action);
            DrainQueue();
        }

        public void SynchroniseSnapshot()
        {
            if (_session == null || _session.Board == null) return;

            RunSnapshot snapshot = _session.Board.CreateSnapshot();
            for (int i = 0; i < snapshot.Cells.Length; i++)
            {
                JellyColour colour = snapshot.Cells[i];
                _renderedColours[i] = colour;
                if (colour == JellyColour.Empty) _board[i].ReturnToPool();
                else _board[i].Show(SpriteFor(colour));
            }

            for (int slot = 0; slot < GameSession.RackSize; slot++)
            {
                JellyColour colour = _session.Rack[slot];
                if (colour == JellyColour.Empty) _rack[slot].ReturnToPool();
                else _rack[slot].Show(SpriteFor(colour));
                _rackSelections[slot].gameObject.SetActive(_session.SelectedSlot == slot);
            }

            RefreshColumnFeedback();
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
            _eventQueue.Clear();
            _processing = false;
            _invalidColumn = -1;
            SynchroniseSnapshot();
        }

        private void Update()
        {
            if (_invalidColumn >= 0 && Time.unscaledTime >= _invalidUntil)
            {
                _invalidColumn = -1;
                RefreshColumnFeedback();
            }
        }

        public bool RenderedBoardMatchesModel()
        {
            if (_session?.Board == null || _board == null) return false;
            for (int i = 0; i < _renderedColours.Length; i++)
            {
                JellyColour model = _session.Board.Cells[i];
                if (_renderedColours[i] != model || _board[i].Image.gameObject.activeSelf != (model != JellyColour.Empty))
                    return false;
                if (model != JellyColour.Empty &&
                    (_board[i].Image.sprite != SpriteFor(model) || !_board[i].IsAtRest())) return false;
            }
            return true;
        }

        private void DrainQueue()
        {
            if (_processing) return;
            _processing = true;
            try
            {
                while (_eventQueue.Count > 0)
                {
                    SessionActionResult action = _eventQueue.Dequeue();
                    for (int i = 0; i < action.Events.Count; i++)
                    {
                        SessionEvent sessionEvent = action.Events[i];
                        if (sessionEvent.Type == SessionEventType.RunStarted)
                            _lastProcessedSequence = 0;
                        if (sessionEvent.Sequence > _lastProcessedSequence)
                            _lastProcessedSequence = sessionEvent.Sequence;
                    }

                    ReplaySemanticChanges(action);
                    // Work Order 11 settles immediately. Work Order 13 can add timing around the same
                    // ordered changes before this mandatory reconciliation without changing Core state.
                    SynchroniseSnapshot();
                }
            }
            finally
            {
                _processing = false;
            }
        }

        private void ReplaySemanticChanges(SessionActionResult action)
        {
            for (int i = 0; i < action.Events.Count; i++)
            {
                SessionEvent sessionEvent = action.Events[i];
                if (sessionEvent.Type == SessionEventType.JellyPlaced && sessionEvent.Coordinate.HasValue)
                {
                    BoardCoordinate coordinate = sessionEvent.Coordinate.Value;
                    SetRenderedCell((coordinate.Row * BoardModel.Width) + coordinate.Column,
                        sessionEvent.Colour);
                }
            }

            ReplayResolution(action.Resolution);
            if (action.PressureRow != null)
            {
                for (int row = 0; row < BoardModel.Height - 1; row++)
                {
                    for (int column = 0; column < BoardModel.Width; column++)
                    {
                        int destination = (row * BoardModel.Width) + column;
                        int source = ((row + 1) * BoardModel.Width) + column;
                        SetRenderedCell(destination, _renderedColours[source]);
                    }
                }
                for (int column = 0; column < BoardModel.Width; column++)
                    SetRenderedCell(((BoardModel.Height - 1) * BoardModel.Width) + column,
                        action.PressureRow[column]);
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

        private void SetRenderedCell(int index, JellyColour colour)
        {
            _renderedColours[index] = colour;
            if (colour == JellyColour.Empty) _board[index].ReturnToPool();
            else _board[index].Show(SpriteFor(colour));
        }

        private void RefreshColumnFeedback()
        {
            bool canDrop = _session.State == SessionState.Ready && _session.SelectedSlot >= 0;
            for (int column = 0; column < BoardModel.Width; column++)
            {
                bool legal = _session.State == SessionState.Ready && !_session.Board.IsColumnFull(column);
                _columnButtons[column].interactable = _session.State == SessionState.Ready;
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
            _debugText.text = $"VISUAL SYNC: {(RenderedBoardMatchesModel() ? "OK" : "MISMATCH")}\n" +
                $"ACTIVE: {active}  DUPLICATES: {duplicateReferences}\n" +
                $"QUEUED: {_eventQueue.Count}  EVENT: {_lastProcessedSequence}";
            _debugText.gameObject.SetActive(true);
#else
            if (_debugText != null) _debugText.gameObject.SetActive(false);
#endif
        }

        private Sprite SpriteFor(JellyColour colour)
        {
            int index = (int)colour - 1;
            return index >= 0 && index < _staticSprites.Length ? _staticSprites[index] : null;
        }

        private static void ValidateLength(Array values, int length, string parameterName)
        {
            if (values == null || values.Length != length)
                throw new ArgumentException($"Expected exactly {length} entries.", parameterName);
        }
    }
}
