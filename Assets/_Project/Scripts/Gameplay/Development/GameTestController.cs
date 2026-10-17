using JellyRiot.Core;
using UnityEngine;

namespace JellyRiot.Gameplay.Development
{
    /// <summary>Development-only playable view of the completed deterministic board work.</summary>
    public sealed class GameTestController : MonoBehaviour
    {
        private const ulong InitialSeed = 2002UL;

        private BoardModel _board;
        private JellyColour _nextColour;
        private ulong _seed = InitialSeed;
        private string _status;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _cellStyle;

        private void Awake()
        {
            ResetBoard();
        }

        private void OnGUI()
        {
            EnsureStyles();

            float scale = Mathf.Min(Screen.width / 520f, Screen.height / 860f);
            scale = Mathf.Max(scale, 0.55f);
            float cellSize = 54f * scale;
            float gap = 3f * scale;
            float boardWidth = (BoardModel.Width * cellSize) + ((BoardModel.Width - 1) * gap);
            float left = (Screen.width - boardWidth) * 0.5f;
            float top = 104f * scale;

            GUI.Label(new Rect(0f, 18f * scale, Screen.width, 42f * scale), "JELLY RIOT — CORE PLAYGROUND", _titleStyle);
            GUI.Label(new Rect(0f, 62f * scale, Screen.width, 30f * scale),
                $"Seed {_seed}   •   Next: {_nextColour}   •   {_status}", _labelStyle);

            for (int row = 0; row < BoardModel.Height; row++)
            {
                for (int column = 0; column < BoardModel.Width; column++)
                {
                    var rect = new Rect(
                        left + (column * (cellSize + gap)),
                        top + (row * (cellSize + gap)),
                        cellSize,
                        cellSize);
                    DrawCell(rect, _board.GetCell(column, row));
                }
            }

            float controlsTop = top + (BoardModel.Height * (cellSize + gap)) + (12f * scale);
            for (int column = 0; column < BoardModel.Width; column++)
            {
                var buttonRect = new Rect(left + (column * (cellSize + gap)), controlsTop, cellSize, 42f * scale);
                GUI.enabled = !_board.IsColumnFull(column);
                if (GUI.Button(buttonRect, (column + 1).ToString()))
                {
                    Drop(column);
                }
            }

            GUI.enabled = true;
            float resetWidth = (boardWidth - gap) * 0.5f;
            float resetTop = controlsTop + (54f * scale);
            if (GUI.Button(new Rect(left, resetTop, resetWidth, 42f * scale), "Reset same seed"))
            {
                ResetBoard();
            }

            if (GUI.Button(new Rect(left + resetWidth + gap, resetTop, resetWidth, 42f * scale), "Next seed"))
            {
                _seed++;
                ResetBoard();
            }
        }

        private void Drop(int column)
        {
            MoveResult move = _board.PlaceJelly(column, _nextColour);
            if (!move.Succeeded)
            {
                _status = move.Failure.ToString();
                return;
            }

            BoardResolutionResult resolution = _board.ResolveMatches();
            _status = resolution.WaveCount == 0
                ? "Placed — no match"
                : $"{resolution.WaveCount} wave(s), {resolution.TotalClearedCount} cleared";
            _nextColour = _board.NextGeneratedColour();
        }

        private void ResetBoard()
        {
            _board = new BoardModel(_seed);
            _nextColour = _board.NextGeneratedColour();
            _status = "Choose a column";
        }

        private void DrawCell(Rect rect, JellyColour colour)
        {
            Color previous = GUI.color;
            GUI.color = ColourFor(colour);
            GUI.Box(rect, colour == JellyColour.Empty ? string.Empty : ShortName(colour), _cellStyle);
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
