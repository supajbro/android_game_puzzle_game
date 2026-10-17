using System;
using System.Collections.Generic;
using System.Linq;
using JellyRiot.Core;
using NUnit.Framework;
using UnityEngine;

namespace JellyRiot.Tests.EditMode
{
    public sealed class BoardModelTests
    {
        [Test]
        public void BoardDimensionsAndCoordinatesAreStable()
        {
            var board = CreateEmptyBoard();

            Assert.That(BoardModel.Width, Is.EqualTo(7));
            Assert.That(BoardModel.Height, Is.EqualTo(9));
            Assert.That(board.Cells.Count, Is.EqualTo(63));
            Assert.That(board.GetCell(0, 0), Is.EqualTo(JellyColour.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(() => board.GetCell(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => board.GetCell(7, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => board.GetCell(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => board.GetCell(0, 9));
        }

        [Test]
        public void PlacementUsesLowestEmptyCell()
        {
            var board = CreateEmptyBoard();

            MoveResult result = board.PlaceJelly(3, JellyColour.Coral);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.PlacedAt, Is.EqualTo(new BoardCoordinate(3, 8)));
            Assert.That(board.GetCell(3, 8), Is.EqualTo(JellyColour.Coral));
            Assert.That(result.Events.Count, Is.EqualTo(1));
        }

        [Test]
        public void PiecesStackFromBottomUpwards()
        {
            var board = CreateEmptyBoard();

            board.PlaceJelly(2, JellyColour.Coral);
            MoveResult second = board.PlaceJelly(2, JellyColour.Lemon);
            MoveResult third = board.PlaceJelly(2, JellyColour.Aqua);

            Assert.That(second.PlacedAt.Value.Row, Is.EqualTo(7));
            Assert.That(third.PlacedAt.Value.Row, Is.EqualTo(6));
            Assert.That(board.GetCell(2, 8), Is.EqualTo(JellyColour.Coral));
            Assert.That(board.GetCell(2, 7), Is.EqualTo(JellyColour.Lemon));
        }

        [Test]
        public void LeftmostAndRightmostColumnsAcceptPlacement()
        {
            var board = CreateEmptyBoard();

            Assert.That(board.PlaceJelly(0, JellyColour.Coral).PlacedAt,
                Is.EqualTo(new BoardCoordinate(0, 8)));
            Assert.That(board.PlaceJelly(6, JellyColour.Lemon).PlacedAt,
                Is.EqualTo(new BoardCoordinate(6, 8)));
        }

        [Test]
        public void FullColumnIsRejectedWithoutMutation()
        {
            var board = CreateEmptyBoard();
            for (int i = 0; i < BoardModel.Height; i++)
            {
                Assert.That(board.PlaceJelly(1, JellyColour.Coral).Succeeded, Is.True);
            }

            JellyColour[] cellsBefore = board.CopyCells();
            int turnBefore = board.TurnCount;
            DeterministicRandomState randomBefore = board.RandomState;
            MoveResult result = board.PlaceJelly(1, JellyColour.Lemon);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.EqualTo(PlacementFailure.ColumnFull));
            Assert.That(board.CopyCells(), Is.EqualTo(cellsBefore));
            Assert.That(board.TurnCount, Is.EqualTo(turnBefore));
            Assert.That(board.RandomState, Is.EqualTo(randomBefore));
        }

        [TestCase(-1)]
        [TestCase(7)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void InvalidColumnIsRejected(int column)
        {
            var board = new BoardModel(71UL);
            Assert.That(board.PlaceJelly(column, JellyColour.Coral).Failure,
                Is.EqualTo(PlacementFailure.InvalidColumn));
        }

        [Test]
        public void InvalidActionsDoNotChangeBoardTurnOrRandomState()
        {
            var board = new BoardModel(901UL);
            JellyColour[] cellsBefore = board.CopyCells();
            int turnBefore = board.TurnCount;
            DeterministicRandomState randomBefore = board.RandomState;

            board.PlaceJelly(-1, JellyColour.Coral);
            board.PlaceJelly(2, JellyColour.Empty);
            board.PlaceJelly(2, JellyColour.Mint);

            Assert.That(board.CopyCells(), Is.EqualTo(cellsBefore));
            Assert.That(board.TurnCount, Is.EqualTo(turnBefore));
            Assert.That(board.RandomState, Is.EqualTo(randomBefore));
        }

        [Test]
        public void IdenticalSeedsProduceIdenticalBoardsAndColourStreams()
        {
            var first = new BoardModel(123456789UL);
            var second = new BoardModel(123456789UL);

            Assert.That(second.CopyCells(), Is.EqualTo(first.CopyCells()));
            for (int i = 0; i < 50; i++)
            {
                Assert.That(second.NextGeneratedColour(), Is.EqualTo(first.NextGeneratedColour()));
            }
        }

        [Test]
        public void DifferentSeedsChangeBoardOrFutureStream()
        {
            var first = new BoardModel(1UL);
            var second = new BoardModel(2UL);

            bool boardDiffers = !first.CopyCells().SequenceEqual(second.CopyCells());
            bool streamDiffers = false;
            for (int i = 0; i < 20; i++)
            {
                streamDiffers |= first.NextGeneratedColour() != second.NextGeneratedColour();
            }

            Assert.That(boardDiffers || streamDiffers, Is.True);
        }

        [Test]
        public void DeterministicRandomReproducesSequenceFromSeed()
        {
            var first = new DeterministicRandom(42UL);
            var second = new DeterministicRandom(42UL);

            for (int i = 0; i < 100; i++)
            {
                Assert.That(second.NextUInt(), Is.EqualTo(first.NextUInt()));
            }
        }

        [Test]
        public void RandomStateCanBeSerialisedAndRestored()
        {
            var source = new DeterministicRandom(8128UL);
            for (int i = 0; i < 17; i++)
            {
                source.NextUInt();
            }

            DeterministicRandomState saved = source.State;
            uint[] expected = Enumerable.Range(0, 25).Select(_ => source.NextUInt()).ToArray();
            var restored = new DeterministicRandom(saved);
            uint[] actual = Enumerable.Range(0, 25).Select(_ => restored.NextUInt()).ToArray();

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void ResetRestoresOriginalBoardAndRandomState()
        {
            var board = new BoardModel(314159UL);
            JellyColour[] initialCells = board.CopyCells();
            DeterministicRandomState initialRandom = board.RandomState;
            board.NextGeneratedColour();
            board.PlaceJelly(0, JellyColour.Coral);

            board.Reset(314159UL);

            Assert.That(board.CopyCells(), Is.EqualTo(initialCells));
            Assert.That(board.RandomState, Is.EqualTo(initialRandom));
            Assert.That(board.TurnCount, Is.Zero);
        }

        [Test]
        public void BoardReadAccessDoesNotExposeMutableBackingArray()
        {
            var board = new BoardModel(77UL);
            JellyColour original = board.GetCell(0, 8);
            JellyColour[] copy = board.CopyCells();
            copy[8 * BoardModel.Width] = JellyColour.Mint;

            Assert.That(board.GetCell(0, 8), Is.EqualTo(original));
            Assert.That(board.Cells, Is.Not.InstanceOf<JellyColour[]>());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<JellyColour>)board.Cells)[0] = JellyColour.Coral);
        }

        [TestCase(0UL)]
        [TestCase(1UL)]
        [TestCase(42UL)]
        [TestCase(999999UL)]
        public void InitialBoardHasValidOccupancyAndDistribution(ulong seed)
        {
            var board = new BoardModel(seed);
            JellyColour[] cells = board.CopyCells();
            int occupied = cells.Count(cell => cell != JellyColour.Empty);
            int distinctColours = cells.Where(cell => cell != JellyColour.Empty).Distinct().Count();

            Assert.That(occupied, Is.InRange(16, 22));
            Assert.That(occupied / (double)BoardModel.CellCount, Is.InRange(0.25, 0.35));
            Assert.That(distinctColours, Is.EqualTo(3));
            for (int row = 0; row < 5; row++)
            {
                for (int column = 0; column < BoardModel.Width; column++)
                {
                    Assert.That(board.GetCell(column, row), Is.EqualTo(JellyColour.Empty));
                }
            }

            Assert.That(board.TryFindSensibleOpening(out int openingColumn, out JellyColour openingColour), Is.True);
            Assert.That(openingColumn, Is.InRange(0, 6));
            Assert.That(openingColour, Is.Not.EqualTo(JellyColour.Empty));
        }

        [Test]
        public void InitialBoardsContainNoPreExistingGroupOfFour()
        {
            for (ulong seed = 0; seed < 1000; seed++)
            {
                var board = new BoardModel(seed);
                Assert.That(board.HasConnectedGroup(4), Is.False, $"Seed {seed} contained an initial match.\n{BoardAsciiPrinter.Format(board)}");
            }
        }

        [Test]
        public void SnapshotRestoresBoardTurnAndFutureRandomSequence()
        {
            var original = new BoardModel(2026UL);
            original.PlaceJelly(0, JellyColour.Coral);
            RunSnapshot snapshot = original.CreateSnapshot();
            var restored = new BoardModel(5UL);

            restored.Restore(snapshot);

            Assert.That(restored.CopyCells(), Is.EqualTo(original.CopyCells()));
            Assert.That(restored.TurnCount, Is.EqualTo(original.TurnCount));
            for (int i = 0; i < 20; i++)
            {
                Assert.That(restored.NextGeneratedColour(), Is.EqualTo(original.NextGeneratedColour()));
            }
        }

        [Test]
        public void SnapshotAndRandomStateSurviveUnityJsonRoundTrip()
        {
            var original = new BoardModel(8675309UL);
            original.PlaceJelly(4, JellyColour.Lemon);
            RunSnapshot snapshot = original.CreateSnapshot();

            string json = JsonUtility.ToJson(snapshot);
            RunSnapshot decoded = JsonUtility.FromJson<RunSnapshot>(json);
            var restored = new BoardModel(1UL);
            restored.Restore(decoded);

            Assert.That(restored.CopyCells(), Is.EqualTo(original.CopyCells()));
            Assert.That(restored.TurnCount, Is.EqualTo(original.TurnCount));
            Assert.That(restored.RandomState, Is.EqualTo(original.RandomState));
            Assert.That(restored.NextGeneratedColour(), Is.EqualTo(original.NextGeneratedColour()));
        }

        [Test]
        public void AsciiPrinterShowsDimensionsAndEmptyCells()
        {
            string output = BoardAsciiPrinter.Format(CreateEmptyBoard());

            Assert.That(output, Does.Contain("7x9"));
            Assert.That(output, Does.Contain("0 1 2 3 4 5 6"));
            Assert.That(output.Count(character => character == '.'), Is.EqualTo(BoardModel.CellCount));
        }

        private static BoardModel CreateEmptyBoard()
        {
            var board = new BoardModel(100UL);
            var empty = new JellyColour[BoardModel.CellCount];
            var snapshot = new RunSnapshot(board.Seed, 0, board.ActiveColourCount, board.RandomState, empty);
            board.Restore(snapshot);
            return board;
        }
    }
}
