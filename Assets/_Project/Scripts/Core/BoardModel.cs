using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JellyRiot.Core
{
    /// <summary>Authoritative 7x9 deterministic board. It contains no presentation or timing concerns.</summary>
    public sealed class BoardModel
    {
        public const int Width = 7;
        public const int Height = 9;
        public const int CellCount = Width * Height;

        private readonly JellyColour[] _cells = new JellyColour[CellCount];
        private readonly ReadOnlyCollection<JellyColour> _readOnlyCells;
        private DeterministicRandom _random;

        public BoardModel(ulong seed) : this(seed, GameBalance.Default)
        {
        }

        public BoardModel(ulong seed, GameBalance balance)
        {
            Balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _readOnlyCells = Array.AsReadOnly(_cells);
            Reset(seed);
        }

        public GameBalance Balance { get; }

        public ulong Seed { get; private set; }

        public int TurnCount { get; private set; }

        public int ActiveColourCount => Balance.InitialActiveColours;

        public IReadOnlyList<JellyColour> Cells => _readOnlyCells;

        public DeterministicRandomState RandomState => _random.State;

        public JellyColour GetCell(int column, int row)
        {
            ValidateCoordinates(column, row);
            return _cells[ToIndex(column, row)];
        }

        public JellyColour[] CopyCells() => (JellyColour[])_cells.Clone();

        public bool IsColumnFull(int column)
        {
            ValidateColumn(column);
            return _cells[ToIndex(column, 0)] != JellyColour.Empty;
        }

        public MoveResult PlaceJelly(int column, JellyColour colour)
        {
            if (column < 0 || column >= Width)
            {
                return MoveResult.Rejected(PlacementFailure.InvalidColumn, TurnCount);
            }

            if (colour == JellyColour.Empty || (int)colour > ActiveColourCount)
            {
                return MoveResult.Rejected(PlacementFailure.InvalidColour, TurnCount);
            }

            for (int row = Height - 1; row >= 0; row--)
            {
                int index = ToIndex(column, row);
                if (_cells[index] != JellyColour.Empty)
                {
                    continue;
                }

                _cells[index] = colour;
                TurnCount++;
                return MoveResult.Placed(new BoardCoordinate(column, row), colour, TurnCount);
            }

            return MoveResult.Rejected(PlacementFailure.ColumnFull, TurnCount);
        }

        public JellyColour NextGeneratedColour()
        {
            return (JellyColour)(1 + _random.NextInt(ActiveColourCount));
        }

        public void Reset(ulong seed)
        {
            Seed = seed;
            TurnCount = 0;
            Array.Clear(_cells, 0, _cells.Length);
            _random = new DeterministicRandom(seed);
            GenerateInitialBoard();
        }

        public RunSnapshot CreateSnapshot() =>
            new RunSnapshot(Seed, TurnCount, ActiveColourCount, _random.State, _cells);

        public void Restore(RunSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (snapshot.ActiveColourCount != ActiveColourCount)
            {
                throw new ArgumentException("Snapshot balance does not match this board.", nameof(snapshot));
            }

            JellyColour[] cells = snapshot.CopyCells();
            if (cells.Length != CellCount)
            {
                throw new ArgumentException("Snapshot has an invalid board size.", nameof(snapshot));
            }

            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] < JellyColour.Empty || (int)cells[i] > ActiveColourCount)
                {
                    throw new ArgumentException("Snapshot contains an invalid jelly colour.", nameof(snapshot));
                }
            }

            Array.Copy(cells, _cells, CellCount);
            Seed = snapshot.Seed;
            TurnCount = snapshot.TurnCount;
            _random = new DeterministicRandom(snapshot.RandomState);
        }

        public bool HasConnectedGroup(int minimumSize)
        {
            if (minimumSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumSize));
            }

            var visited = new bool[CellCount];
            for (int index = 0; index < CellCount; index++)
            {
                JellyColour colour = _cells[index];
                if (colour == JellyColour.Empty || visited[index])
                {
                    continue;
                }

                if (CountConnected(index, colour, visited, minimumSize) >= minimumSize)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryFindSensibleOpening(out int column, out JellyColour colour)
        {
            for (int candidateColumn = 0; candidateColumn < Width; candidateColumn++)
            {
                int targetRow = FindLowestEmptyRow(candidateColumn);
                if (targetRow < 0)
                {
                    continue;
                }

                for (int value = 1; value <= ActiveColourCount; value++)
                {
                    JellyColour candidate = (JellyColour)value;
                    _cells[ToIndex(candidateColumn, targetRow)] = candidate;
                    bool createsMatch = HasGroupContaining(candidateColumn, targetRow, 4);
                    _cells[ToIndex(candidateColumn, targetRow)] = JellyColour.Empty;
                    if (createsMatch)
                    {
                        column = candidateColumn;
                        colour = candidate;
                        return true;
                    }
                }
            }

            column = -1;
            colour = JellyColour.Empty;
            return false;
        }

        private void GenerateInitialBoard()
        {
            int openingColumn = _random.NextInt(Width);
            JellyColour openingColour = (JellyColour)(1 + _random.NextInt(ActiveColourCount));

            var heights = new int[Width];
            for (int column = 0; column < Width; column++)
            {
                heights[column] = 2;
            }

            heights[openingColumn] = 3;
            int allocated = (Width * 2) + 1;
            while (allocated < Balance.InitialOccupiedCells)
            {
                int column = _random.NextInt(Width);
                int maximumHeight = column == openingColumn ? 3 : 4;
                if (heights[column] >= maximumHeight)
                {
                    continue;
                }

                heights[column]++;
                allocated++;
            }

            for (int row = Height - 3; row < Height; row++)
            {
                _cells[ToIndex(openingColumn, row)] = openingColour;
            }

            var positions = new int[Balance.InitialOccupiedCells - 3];
            var colourStarts = new int[positions.Length];
            int positionCount = 0;
            for (int column = 0; column < Width; column++)
            {
                for (int offset = 0; offset < heights[column]; offset++)
                {
                    int row = Height - 1 - offset;
                    if (_cells[ToIndex(column, row)] != JellyColour.Empty)
                    {
                        continue;
                    }

                    positions[positionCount] = ToIndex(column, row);
                    colourStarts[positionCount] = _random.NextInt(ActiveColourCount);
                    positionCount++;
                }
            }

            if (positionCount != positions.Length || !FillGeneratedCells(0, positions, colourStarts))
            {
                throw new InvalidOperationException("Could not generate a safe initial board.");
            }

            if (HasConnectedGroup(4) || !TryFindSensibleOpening(out _, out _))
            {
                throw new InvalidOperationException("Initial board generation violated its deterministic safety contract.");
            }
        }

        private bool FillGeneratedCells(int position, int[] positions, int[] colourStarts)
        {
            if (position >= positions.Length)
            {
                return true;
            }

            int index = positions[position];
            int column = index % Width;
            int row = index / Width;
            for (int offset = 0; offset < ActiveColourCount; offset++)
            {
                JellyColour candidate = (JellyColour)(1 + ((colourStarts[position] + offset) % ActiveColourCount));
                _cells[index] = candidate;
                if (!HasGroupContaining(column, row, 4) &&
                    FillGeneratedCells(position + 1, positions, colourStarts))
                {
                    return true;
                }
            }

            _cells[index] = JellyColour.Empty;
            return false;
        }

        private int CountConnected(int startIndex, JellyColour colour, bool[] visited, int stopAt)
        {
            var queue = new int[CellCount];
            int head = 0;
            int tail = 0;
            int count = 0;
            queue[tail++] = startIndex;
            visited[startIndex] = true;

            while (head < tail)
            {
                int index = queue[head++];
                count++;
                if (count >= stopAt)
                {
                    return count;
                }

                int column = index % Width;
                int row = index / Width;
                AddNeighbour(column - 1, row, colour, visited, queue, ref tail);
                AddNeighbour(column + 1, row, colour, visited, queue, ref tail);
                AddNeighbour(column, row - 1, colour, visited, queue, ref tail);
                AddNeighbour(column, row + 1, colour, visited, queue, ref tail);
            }

            return count;
        }

        private bool HasGroupContaining(int column, int row, int minimumSize)
        {
            int index = ToIndex(column, row);
            JellyColour colour = _cells[index];
            return colour != JellyColour.Empty &&
                   CountConnected(index, colour, new bool[CellCount], minimumSize) >= minimumSize;
        }

        private void AddNeighbour(int column, int row, JellyColour colour, bool[] visited, int[] queue, ref int tail)
        {
            if (column < 0 || column >= Width || row < 0 || row >= Height)
            {
                return;
            }

            int index = ToIndex(column, row);
            if (visited[index] || _cells[index] != colour)
            {
                return;
            }

            visited[index] = true;
            queue[tail++] = index;
        }

        private int FindLowestEmptyRow(int column)
        {
            for (int row = Height - 1; row >= 0; row--)
            {
                if (_cells[ToIndex(column, row)] == JellyColour.Empty)
                {
                    return row;
                }
            }

            return -1;
        }

        private static int ToIndex(int column, int row) => (row * Width) + column;

        private static void ValidateCoordinates(int column, int row)
        {
            if (column < 0 || column >= Width)
            {
                throw new ArgumentOutOfRangeException(nameof(column), column, $"Column must be 0-{Width - 1}.");
            }

            if (row < 0 || row >= Height)
            {
                throw new ArgumentOutOfRangeException(nameof(row), row, $"Row must be 0-{Height - 1}.");
            }
        }

        private static void ValidateColumn(int column)
        {
            if (column < 0 || column >= Width)
            {
                throw new ArgumentOutOfRangeException(nameof(column), column, $"Column must be 0-{Width - 1}.");
            }
        }
    }
}
