using System;
using System.Collections.Generic;

namespace JellyRiot.Core
{
    /// <summary>Deterministic match, simultaneous-clear and stable-gravity pipeline.</summary>
    internal static class BoardResolver
    {
        private const int MatchSize = 4;
        private const int MaximumWaveCount = (BoardModel.CellCount / MatchSize) + 1;

        public static BoardResolutionResult Resolve(BoardModel board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var visited = new bool[BoardModel.CellCount];
            var cellsToClear = new bool[BoardModel.CellCount];
            var queue = new int[BoardModel.CellCount];
            var component = new int[BoardModel.CellCount];
            var waves = new List<ResolutionWave>(4);
            var groupSizes = new List<int>(4);

            for (int waveIndex = 0; waveIndex < MaximumWaveCount; waveIndex++)
            {
                Array.Clear(visited, 0, visited.Length);
                Array.Clear(cellsToClear, 0, cellsToClear.Length);
                groupSizes.Clear();

                if (!FindMatches(board, visited, cellsToClear, queue, component, groupSizes))
                {
                    return waves.Count == 0 ? BoardResolutionResult.Empty : new BoardResolutionResult(waves);
                }

                var cleared = ClearMatches(board, cellsToClear);
                if (cleared.Count < MatchSize)
                {
                    throw new InvalidOperationException("A resolution wave did not clear the minimum match size.");
                }

                List<FallingJelly> falls = ApplyGravity(board);
                waves.Add(new ResolutionWave(waveIndex, cleared, falls, groupSizes));
            }

            throw new InvalidOperationException("Board resolution exceeded its mathematical wave limit.");
        }

        private static bool FindMatches(BoardModel board, bool[] visited, bool[] cellsToClear,
            int[] queue, int[] component, List<int> groupSizes)
        {
            bool foundMatch = false;
            for (int start = 0; start < BoardModel.CellCount; start++)
            {
                JellyColour colour = board.GetCellUnchecked(start);
                if (colour == JellyColour.Empty || visited[start])
                {
                    continue;
                }

                int head = 0;
                int tail = 0;
                int componentCount = 0;
                queue[tail++] = start;
                visited[start] = true;

                while (head < tail)
                {
                    int index = queue[head++];
                    component[componentCount++] = index;
                    int column = index % BoardModel.Width;
                    int row = index / BoardModel.Width;

                    EnqueueIfMatching(board, column - 1, row, colour, visited, queue, ref tail);
                    EnqueueIfMatching(board, column + 1, row, colour, visited, queue, ref tail);
                    EnqueueIfMatching(board, column, row - 1, colour, visited, queue, ref tail);
                    EnqueueIfMatching(board, column, row + 1, colour, visited, queue, ref tail);
                }

                if (componentCount < MatchSize)
                {
                    continue;
                }

                foundMatch = true;
                groupSizes.Add(componentCount);
                for (int i = 0; i < componentCount; i++)
                {
                    cellsToClear[component[i]] = true;
                }
            }

            return foundMatch;
        }

        private static void EnqueueIfMatching(BoardModel board, int column, int row, JellyColour colour,
            bool[] visited, int[] queue, ref int tail)
        {
            if (column < 0 || column >= BoardModel.Width || row < 0 || row >= BoardModel.Height)
            {
                return;
            }

            int index = (row * BoardModel.Width) + column;
            if (visited[index] || board.GetCellUnchecked(index) != colour)
            {
                return;
            }

            visited[index] = true;
            queue[tail++] = index;
        }

        private static List<ClearedJelly> ClearMatches(BoardModel board, bool[] cellsToClear)
        {
            var cleared = new List<ClearedJelly>(BoardModel.CellCount);
            for (int index = 0; index < BoardModel.CellCount; index++)
            {
                if (!cellsToClear[index])
                {
                    continue;
                }

                JellyColour colour = board.GetCellUnchecked(index);
                int column = index % BoardModel.Width;
                int row = index / BoardModel.Width;
                cleared.Add(new ClearedJelly(new BoardCoordinate(column, row), colour));
            }

            // Mutation happens only after every qualifying component has been identified and recorded.
            for (int index = 0; index < BoardModel.CellCount; index++)
            {
                if (cellsToClear[index])
                {
                    board.SetCellUnchecked(index, JellyColour.Empty);
                }
            }

            return cleared;
        }

        private static List<FallingJelly> ApplyGravity(BoardModel board)
        {
            var falls = new List<FallingJelly>(BoardModel.CellCount);
            for (int column = 0; column < BoardModel.Width; column++)
            {
                int destinationRow = BoardModel.Height - 1;
                for (int sourceRow = BoardModel.Height - 1; sourceRow >= 0; sourceRow--)
                {
                    int sourceIndex = (sourceRow * BoardModel.Width) + column;
                    JellyColour colour = board.GetCellUnchecked(sourceIndex);
                    if (colour == JellyColour.Empty)
                    {
                        continue;
                    }

                    if (sourceRow != destinationRow)
                    {
                        int destinationIndex = (destinationRow * BoardModel.Width) + column;
                        board.SetCellUnchecked(destinationIndex, colour);
                        board.SetCellUnchecked(sourceIndex, JellyColour.Empty);
                        falls.Add(new FallingJelly(
                            new BoardCoordinate(column, sourceRow),
                            new BoardCoordinate(column, destinationRow),
                            colour));
                    }

                    destinationRow--;
                }

                for (int row = destinationRow; row >= 0; row--)
                {
                    board.SetCellUnchecked((row * BoardModel.Width) + column, JellyColour.Empty);
                }
            }

            return falls;
        }
    }
}
