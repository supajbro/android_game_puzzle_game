#if UNITY_EDITOR
using JellyRiot.Core;
using UnityEngine;

namespace JellyRiot.Gameplay.Development
{
    /// <summary>Editor-only input harness for the current deterministic Core implementation.</summary>
    public sealed class GameTestController : MonoBehaviour
    {
        private const ulong InitialSeed = 2002UL;

        private GameSession _session;
        private ulong _seed = InitialSeed;
        private string _status;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _cellStyle;

        private void Awake()
        {
            StartSession();
        }

        private void OnGUI()
        {
            EnsureStyles();

            float scale = Mathf.Max(Mathf.Min(Screen.width / 520f, Screen.height / 940f), 0.5f);
            float cellSize = 54f * scale;
            float gap = 3f * scale;
            float boardWidth = (BoardModel.Width * cellSize) + ((BoardModel.Width - 1) * gap);
            float left = (Screen.width - boardWidth) * 0.5f;
            float top = 112f * scale;

            GUI.Label(new Rect(0f, 14f * scale, Screen.width, 40f * scale),
                "JELLY RIOT — CORE PLAYGROUND", _titleStyle);
            GUI.Label(new Rect(0f, 54f * scale, Screen.width, 48f * scale),
                $"Seed {_seed}   •   Score {_session.Score.CurrentScore}\n{_status}", _labelStyle);

            GUI.Label(new Rect(0f, 88f * scale, Screen.width, 24f * scale),
                $"Next pressure row in {_session.NextRowIn} turn(s)", _labelStyle);

            for (int row = 0; row < BoardModel.Height; row++)
            {
                for (int column = 0; column < BoardModel.Width; column++)
                {
                    var rect = new Rect(left + (column * (cellSize + gap)),
                        top + (row * (cellSize + gap)), cellSize, cellSize);
                    DrawCell(rect, _session.Board.GetCell(column, row), false);
                }
            }

            float columnTop = top + (BoardModel.Height * (cellSize + gap)) + (10f * scale);
            for (int column = 0; column < BoardModel.Width; column++)
            {
                var buttonRect = new Rect(left + (column * (cellSize + gap)), columnTop,
                    cellSize, 38f * scale);
                GUI.enabled = _session.State == SessionState.Ready &&
                              !_session.Board.IsColumnFull(column) && _session.SelectedSlot >= 0;
                if (GUI.Button(buttonRect, (column + 1).ToString()))
                {
                    PlayTurn(column);
                }
            }

            GUI.enabled = true;
            float rackTop = columnTop + (48f * scale);
            float rackCell = 62f * scale;
            float rackGap = 12f * scale;
            float rackWidth = (GameSession.RackSize * rackCell) + ((GameSession.RackSize - 1) * rackGap);
            float rackLeft = (Screen.width - rackWidth) * 0.5f;
            for (int slot = 0; slot < GameSession.RackSize; slot++)
            {
                var rect = new Rect(rackLeft + (slot * (rackCell + rackGap)), rackTop, rackCell, rackCell);
                GUI.enabled = _session.State == SessionState.Ready;
                if (GUI.Button(rect, string.Empty))
                {
                    _session.SelectRackSlot(slot);
                    _status = $"Selected slot {slot + 1}: {_session.Rack[slot]}";
                }

                DrawCell(rect, _session.Rack[slot], _session.SelectedSlot == slot);
            }

            GUI.enabled = true;
            float resetTop = rackTop + rackCell + (12f * scale);
            float resetWidth = (boardWidth - gap) * 0.5f;
            if (GUI.Button(new Rect(left, resetTop, resetWidth, 38f * scale), "Reset same seed"))
            {
                _session.ResetRun();
                _status = "Run reset — select a rack slot";
            }

            if (GUI.Button(new Rect(left + resetWidth + gap, resetTop, resetWidth, 38f * scale), "Next seed"))
            {
                _seed++;
                StartSession();
            }

            if (_session.State == SessionState.PendingLoss)
            {
                float actionTop = resetTop + (46f * scale);
                if (GUI.Button(new Rect(left, actionTop, resetWidth, 38f * scale), "Restore checkpoint"))
                {
                    _session.Restore(_session.ContinueCheckpoint);
                    _status = "Checkpoint restored - choose a different move";
                }

                if (GUI.Button(new Rect(left + resetWidth + gap, actionTop, resetWidth, 38f * scale),
                        "Confirm game over"))
                {
                    _session.ConfirmGameOver();
                    _status = $"Game over - final score {_session.CurrentResult.FinalScore}";
                }
            }
        }

        private void PlayTurn(int column)
        {
            SessionActionResult started = _session.TryBeginTurn(column);
            if (!started.Succeeded)
            {
                _status = started.Failure.ToString();
                return;
            }

            SessionActionResult completed = _session.CompleteTurn();
            if (_session.State == SessionState.PendingLoss)
            {
                _status = "Pressure blocked - restore checkpoint or confirm game over";
                return;
            }

            _status = completed.Resolution.WaveCount == 0
                ? "Placed — no match. Select another slot."
                : $"{completed.Resolution.WaveCount} wave(s), " +
                  $"{completed.Resolution.TotalClearedCount} cleared. Select another slot.";
        }

        private void StartSession()
        {
            _session = new GameSession();
            _session.CompleteBoot();
            _session.StartNewRun(_seed);
            _status = "Select one of the three rack slots";
        }

        private void DrawCell(Rect rect, JellyColour colour, bool selected)
        {
            Color previous = GUI.color;
            GUI.color = selected ? Color.white : ColourFor(colour);
            GUI.Box(rect, selected ? "●" : ShortName(colour), _cellStyle);
            if (selected)
            {
                var inner = new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, rect.height - 10f);
                GUI.color = ColourFor(colour);
                GUI.Box(inner, ShortName(colour), _cellStyle);
            }

            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.98f, 0.95f, 1f) }
            };
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                normal = { textColor = new Color(0.83f, 0.83f, 0.9f) }
            };
            _cellStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        private static Color ColourFor(JellyColour colour)
        {
            switch (colour)
            {
                case JellyColour.Coral:
                    return new Color(1f, 0.31f, 0.39f);
                case JellyColour.Lemon:
                    return new Color(1f, 0.78f, 0.12f);
                case JellyColour.Aqua:
                    return new Color(0.08f, 0.76f, 0.84f);
                default:
                    return new Color(0.16f, 0.16f, 0.22f);
            }
        }

        private static string ShortName(JellyColour colour)
        {
            switch (colour)
            {
                case JellyColour.Coral:
                    return "C";
                case JellyColour.Lemon:
                    return "L";
                case JellyColour.Aqua:
                    return "A";
                default:
                    return string.Empty;
            }
        }
    }
}
#endif
