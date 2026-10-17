using System;
using System.Collections.Generic;
using JellyRiot.Core;
using NUnit.Framework;

namespace JellyRiot.Tests.EditMode
{
    public sealed class BoardResolverTests
    {
        [Test]
        public void HorizontalGroupOfFourClears()
        {
            BoardModel board = CreateBoard(
                Cell(0, 8, JellyColour.Coral), Cell(1, 8, JellyColour.Coral),
                Cell(2, 8, JellyColour.Coral), Cell(3, 8, JellyColour.Coral));

            BoardResolutionResult result = board.ResolveMatches();

            Assert.That(result.WaveCount, Is.EqualTo(1));
            Assert.That(result.TotalClearedCount, Is.EqualTo(4));
            AssertBoardEmpty(board);
        }

        [Test]
        public void VerticalGroupOfFourClears()
        {
            BoardModel board = CreateBoard(
                Cell(2, 5, JellyColour.Lemon), Cell(2, 6, JellyColour.Lemon),
                Cell(2, 7, JellyColour.Lemon), Cell(2, 8, JellyColour.Lemon));

            BoardResolutionResult result = board.ResolveMatches();

            Assert.That(result.TotalClearedCount, Is.EqualTo(4));
            AssertBoardEmpty(board);
        }

        [Test]
        public void LShapedGroupClears()
        {
            BoardModel board = CreateBoard(
                Cell(0, 6, JellyColour.Aqua), Cell(0, 7, JellyColour.Aqua),
                Cell(0, 8, JellyColour.Aqua), Cell(1, 8, JellyColour.Aqua));

            Assert.That(board.ResolveMatches().TotalClearedCount, Is.EqualTo(4));
            AssertBoardEmpty(board);
        }

        [Test]
        public void ConnectedGroupLargerThanFourClearsInOneWave()
        {
            BoardModel board = CreateBoard(
                Cell(1, 7, JellyColour.Coral), Cell(2, 7, JellyColour.Coral),
                Cell(3, 7, JellyColour.Coral), Cell(2, 6, JellyColour.Coral),
                Cell(2, 8, JellyColour.Coral), Cell(3, 8, JellyColour.Coral));

            BoardResolutionResult result = board.ResolveMatches();

            Assert.That(result.WaveCount, Is.EqualTo(1));
            Assert.That(result.Waves[0].ClearedCount, Is.EqualTo(6));
        }

        [Test]
        public void DiagonalJelliesDoNotMatch()
        {
            BoardModel board = CreateBoard(
                Cell(0, 5, JellyColour.Lemon), Cell(1, 6, JellyColour.Lemon),
                Cell(2, 7, JellyColour.Lemon), Cell(3, 8, JellyColour.Lemon));
            JellyColour[] before = board.CopyCells();

            BoardResolutionResult result = board.ResolveMatches();

            Assert.That(result.WaveCount, Is.Zero);
            Assert.That(board.CopyCells(), Is.EqualTo(before));
        }

        [Test]
        public void SeparateGroupsClearSimultaneously()
        {
            BoardModel board = CreateBoard(
                Cell(0, 8, JellyColour.Coral), Cell(1, 8, JellyColour.Coral),
                Cell(2, 8, JellyColour.Coral), Cell(3, 8, JellyColour.Coral),
                Cell(6, 5, JellyColour.Lemon), Cell(6, 6, JellyColour.Lemon),
                Cell(6, 7, JellyColour.Lemon), Cell(6, 8, JellyColour.Lemon));

            BoardResolutionResult result = board.ResolveMatches();

            Assert.That(result.WaveCount, Is.EqualTo(1));
            Assert.That(result.Waves[0].ClearedCount, Is.EqualTo(8));
            AssertBoardEmpty(board);
        }

        [Test]
        public void GravityCreatedMatchResolvesAsSecondWave()
        {
            BoardModel board = CreateChainBoard();

            BoardResolutionResult result = board.ResolveMatches();

            Assert.That(result.WaveCount, Is.EqualTo(2));
            Assert.That(result.Waves[0].ClearedCount, Is.EqualTo(4));
            Assert.That(result.Waves[1].ClearedCount, Is.EqualTo(4));
            AssertBoardEmpty(board);
        }

        [Test]
        public void GravityPreservesVerticalOrder()
        {
            BoardModel board = CreateBoard(
                Cell(1, 8, JellyColour.Coral), Cell(2, 8, JellyColour.Coral),
                Cell(3, 8, JellyColour.Coral), Cell(4, 8, JellyColour.Coral),
                Cell(1, 7, JellyColour.Aqua), Cell(1, 6, JellyColour.Lemon));

            ResolutionWave wave = board.ResolveMatches().Waves[0];

            Assert.That(board.GetCell(1, 8), Is.EqualTo(JellyColour.Aqua));
            Assert.That(board.GetCell(1, 7), Is.EqualTo(JellyColour.Lemon));
            Assert.That(wave.Falls.Count, Is.EqualTo(2));
            Assert.That(wave.Falls[0], Is.EqualTo(new FallingJelly(
                new BoardCoordinate(1, 7), new BoardCoordinate(1, 8), JellyColour.Aqua)));
            Assert.That(wave.Falls[1], Is.EqualTo(new FallingJelly(
                new BoardCoordinate(1, 6), new BoardCoordinate(1, 7), JellyColour.Lemon)));
        }

        [Test]
        public void EmptyBoardProducesEmptyResult()
        {
            BoardResolutionResult result = CreateBoard().ResolveMatches();

            Assert.That(result.WaveCount, Is.Zero);
            Assert.That(result.TotalClearedCount, Is.Zero);
            Assert.That(result.Waves, Is.Empty);
        }

        [Test]
        public void FullSingleColourBoardClearsWithoutRecursionFailure()
        {
            var cells = new TestCell[BoardModel.CellCount];
            int index = 0;
            for (int row = 0; row < BoardModel.Height; row++)
            {
                for (int column = 0; column < BoardModel.Width; column++)
                {
                    cells[index++] = Cell(column, row, JellyColour.Coral);
                }
            }

            BoardModel board = CreateBoard(cells);
            BoardResolutionResult result = board.ResolveMatches();

            Assert.That(result.WaveCount, Is.EqualTo(1));
            Assert.That(result.TotalClearedCount, Is.EqualTo(BoardModel.CellCount));
            AssertBoardEmpty(board);
        }

        [Test]
        public void ConnectedComponentContainsNoDuplicateClearEvents()
        {
            BoardModel board = CreateBoard(
                Cell(2, 6, JellyColour.Aqua), Cell(1, 7, JellyColour.Aqua),
                Cell(2, 7, JellyColour.Aqua), Cell(3, 7, JellyColour.Aqua),
                Cell(2, 8, JellyColour.Aqua));

            ResolutionWave wave = board.ResolveMatches().Waves[0];
            var seen = new HashSet<BoardCoordinate>();
            for (int i = 0; i < wave.Cleared.Count; i++)
            {
                Assert.That(seen.Add(wave.Cleared[i].Position), Is.True);
            }

            Assert.That(seen.Count, Is.EqualTo(5));
        }

        [Test]
        public void EveryMatchedCellClearsExactlyOnce()
        {
            BoardModel board = CreateBoard(
                Cell(0, 7, JellyColour.Coral), Cell(1, 7, JellyColour.Coral),
                Cell(2, 7, JellyColour.Coral), Cell(3, 7, JellyColour.Coral),
                Cell(0, 8, JellyColour.Coral), Cell(1, 8, JellyColour.Coral),
                Cell(2, 8, JellyColour.Coral));

            BoardResolutionResult result = board.ResolveMatches();

            Assert.That(result.TotalClearedCount, Is.EqualTo(7));
            Assert.That(result.Waves[0].ClearedCount, Is.EqualTo(7));
        }

        [Test]
        public void NonMatchingJelliesRemainAfterAnotherGroupClears()
        {
            BoardModel board = CreateBoard(
                Cell(0, 8, JellyColour.Coral), Cell(1, 8, JellyColour.Coral),
                Cell(2, 8, JellyColour.Coral), Cell(3, 8, JellyColour.Coral),
                Cell(6, 8, JellyColour.Lemon));

            board.ResolveMatches();

            Assert.That(board.GetCell(6, 8), Is.EqualTo(JellyColour.Lemon));
        }

        [Test]
        public void GravityNeverMovesJellyHorizontally()
        {
            ResolutionWave wave = CreateBoard(
                Cell(1, 8, JellyColour.Coral), Cell(2, 8, JellyColour.Coral),
                Cell(3, 8, JellyColour.Coral), Cell(4, 8, JellyColour.Coral),
                Cell(1, 6, JellyColour.Lemon), Cell(3, 5, JellyColour.Aqua))
                .ResolveMatches().Waves[0];

            for (int i = 0; i < wave.Falls.Count; i++)
            {
                Assert.That(wave.Falls[i].To.Column, Is.EqualTo(wave.Falls[i].From.Column));
                Assert.That(wave.Falls[i].To.Row, Is.GreaterThan(wave.Falls[i].From.Row));
            }
        }

        [Test]
        public void SettledColumnsContainNoGapBelowOccupiedCell()
        {
            BoardModel board = CreateBoard(
                Cell(0, 8, JellyColour.Coral), Cell(1, 8, JellyColour.Coral),
                Cell(2, 8, JellyColour.Coral), Cell(3, 8, JellyColour.Coral),
                Cell(0, 5, JellyColour.Aqua), Cell(0, 3, JellyColour.Lemon),
                Cell(4, 4, JellyColour.Aqua));

            board.ResolveMatches();

            for (int column = 0; column < BoardModel.Width; column++)
            {
                bool foundEmptyBelow = false;
                for (int row = BoardModel.Height - 1; row >= 0; row--)
                {
                    if (board.GetCell(column, row) == JellyColour.Empty)
                    {
                        foundEmptyBelow = true;
                    }
                    else
                    {
                        Assert.That(foundEmptyBelow, Is.False, $"Gap beneath ({column},{row}).");
                    }
                }
            }
        }

        [Test]
        public void ClearAndFallEventsMatchBoardMutation()
        {
            BoardModel board = CreateBoard(
                Cell(1, 8, JellyColour.Coral), Cell(2, 8, JellyColour.Coral),
                Cell(3, 8, JellyColour.Coral), Cell(4, 8, JellyColour.Coral),
                Cell(2, 6, JellyColour.Lemon));

            ResolutionWave wave = board.ResolveMatches().Waves[0];

            Assert.That(wave.Cleared[0], Is.EqualTo(new ClearedJelly(
                new BoardCoordinate(1, 8), JellyColour.Coral)));
            Assert.That(wave.Falls[0], Is.EqualTo(new FallingJelly(
                new BoardCoordinate(2, 6), new BoardCoordinate(2, 8), JellyColour.Lemon)));
            Assert.That(board.GetCell(2, 8), Is.EqualTo(JellyColour.Lemon));
            Assert.That(board.GetCell(2, 6), Is.EqualTo(JellyColour.Empty));
        }

        [Test]
        public void WaveIndicesAndAggregateCountsAreExact()
        {
            BoardResolutionResult result = CreateChainBoard().ResolveMatches();

            Assert.That(result.Waves[0].Index, Is.Zero);
            Assert.That(result.Waves[1].Index, Is.EqualTo(1));
            Assert.That(result.Waves[0].ClearedCount, Is.EqualTo(result.Waves[0].Cleared.Count));
            Assert.That(result.Waves[1].ClearedCount, Is.EqualTo(result.Waves[1].Cleared.Count));
            Assert.That(result.TotalClearedCount, Is.EqualTo(8));
        }

        [Test]
        public void IdenticalBoardsProduceIdenticalOrderedResults()
        {
            BoardModel first = CreateChainBoard();
            BoardModel second = CreateChainBoard();

            BoardResolutionResult firstResult = first.ResolveMatches();
            BoardResolutionResult secondResult = second.ResolveMatches();

            AssertResultsEqual(firstResult, secondResult);
            Assert.That(second.CopyCells(), Is.EqualTo(first.CopyCells()));
        }

        [Test]
        public void StableBoardReturnsZeroWavesWithoutMutation()
        {
            BoardModel board = CreateBoard(
                Cell(0, 8, JellyColour.Coral), Cell(1, 8, JellyColour.Lemon),
                Cell(2, 8, JellyColour.Aqua), Cell(0, 7, JellyColour.Lemon));
            JellyColour[] before = board.CopyCells();

            BoardResolutionResult result = board.ResolveMatches();

            Assert.That(result.WaveCount, Is.Zero);
            Assert.That(board.CopyCells(), Is.EqualTo(before));
        }

        [Test]
        public void DenseBoardTerminatesWithinMathematicalWaveBound()
        {
            var cells = new List<TestCell>(BoardModel.CellCount);
            for (int row = 0; row < BoardModel.Height; row++)
            {
                for (int column = 0; column < BoardModel.Width; column++)
                {
                    JellyColour colour = row < 4
                        ? JellyColour.Aqua
                        : ((column + row) % 2 == 0 ? JellyColour.Coral : JellyColour.Lemon);
                    cells.Add(Cell(column, row, colour));
                }
            }

            BoardResolutionResult result = CreateBoard(cells.ToArray()).ResolveMatches();

            Assert.That(result.WaveCount, Is.InRange(1, (BoardModel.CellCount / 4)));
        }

        [Test]
        public void ResultCollectionsCannotBeMutatedThroughListInterfaces()
        {
            BoardResolutionResult result = CreateBoard(
                Cell(0, 8, JellyColour.Coral), Cell(1, 8, JellyColour.Coral),
                Cell(2, 8, JellyColour.Coral), Cell(3, 8, JellyColour.Coral))
                .ResolveMatches();

            Assert.Throws<NotSupportedException>(() =>
                ((IList<ResolutionWave>)result.Waves).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<ClearedJelly>)result.Waves[0].Cleared).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<FallingJelly>)result.Waves[0].Falls).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<int>)result.Waves[0].GroupSizes).Clear());
        }

        private static BoardModel CreateChainBoard()
        {
            return CreateBoard(
                Cell(0, 8, JellyColour.Coral), Cell(1, 8, JellyColour.Coral),
                Cell(2, 8, JellyColour.Coral), Cell(3, 8, JellyColour.Coral),
                Cell(0, 7, JellyColour.Lemon), Cell(1, 6, JellyColour.Lemon),
                Cell(2, 5, JellyColour.Lemon), Cell(3, 4, JellyColour.Lemon));
        }

        private static BoardModel CreateBoard(params TestCell[] testCells)
        {
            var board = new BoardModel(2002UL);
            var cells = new JellyColour[BoardModel.CellCount];
            for (int i = 0; i < testCells.Length; i++)
            {
                TestCell cell = testCells[i];
                cells[(cell.Row * BoardModel.Width) + cell.Column] = cell.Colour;
            }

            board.Restore(new RunSnapshot(board.Seed, 0, board.ActiveColourCount, board.RandomState, cells));
            return board;
        }

        private static TestCell Cell(int column, int row, JellyColour colour) =>
            new TestCell(column, row, colour);

        private static void AssertBoardEmpty(BoardModel board)
        {
            for (int i = 0; i < board.Cells.Count; i++)
            {
                Assert.That(board.Cells[i], Is.EqualTo(JellyColour.Empty));
            }
        }

        private static void AssertResultsEqual(BoardResolutionResult expected, BoardResolutionResult actual)
        {
            Assert.That(actual.WaveCount, Is.EqualTo(expected.WaveCount));
            for (int waveIndex = 0; waveIndex < expected.WaveCount; waveIndex++)
            {
                ResolutionWave expectedWave = expected.Waves[waveIndex];
                ResolutionWave actualWave = actual.Waves[waveIndex];
                Assert.That(actualWave.Index, Is.EqualTo(expectedWave.Index));
                Assert.That(actualWave.Cleared, Is.EqualTo(expectedWave.Cleared));
                Assert.That(actualWave.Falls, Is.EqualTo(expectedWave.Falls));
            }
        }

        private readonly struct TestCell
        {
            public TestCell(int column, int row, JellyColour colour)
            {
                Column = column;
                Row = row;
                Colour = colour;
            }

            public int Column { get; }
            public int Row { get; }
            public JellyColour Colour { get; }
        }
    }
}
