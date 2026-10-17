using System;
using JellyRiot.Core;
using NUnit.Framework;

namespace JellyRiot.Tests.EditMode
{
    public sealed class PressureAndGeneratorTests
    {
        [Test]
        public void PressureCadenceUsesSixThenFiveThenFour()
        {
            var balance = new GameBalance();

            Assert.That(balance.FirstPressureTurn, Is.EqualTo(6));
            Assert.That(balance.GetPressureIntervalAfter(6), Is.EqualTo(6));
            Assert.That(balance.GetPressureIntervalAfter(18), Is.EqualTo(5));
            Assert.That(balance.GetPressureIntervalAfter(43), Is.EqualTo(4));
        }

        [Test]
        public void CountdownDecrementsAndResetsAfterPressure()
        {
            var balance = ImmediatePressureBalance();
            GameSession session = CreateSession(50UL, balance);
            SetEmptyBoard(session.Board);

            Assert.That(session.NextRowIn, Is.EqualTo(1));
            Play(session, 0, 6);

            Assert.That(session.NextRowIn, Is.EqualTo(balance.EarlyPressureInterval));
        }

        [Test]
        public void GeneratedRowsContainThreeToFiveFilledCellsAndTwoToFourGaps()
        {
            var balance = new GameBalance();
            var generator = new RunContentGenerator(100UL);
            for (int sample = 0; sample < 200; sample++)
            {
                JellyColour[] row = generator.NextPressureRow(balance, 3);
                int filled = 0;
                for (int column = 0; column < row.Length; column++)
                {
                    if (row[column] != JellyColour.Empty)
                    {
                        filled++;
                        Assert.That((int)row[column], Is.InRange(1, 3));
                    }
                }

                Assert.That(filled, Is.InRange(3, 5));
                Assert.That(BoardModel.Width - filled, Is.InRange(2, 4));
            }
        }

        [Test]
        public void SameGeneratorSeedProducesSameRackAndRows()
        {
            var first = new RunContentGenerator(77UL);
            var second = new RunContentGenerator(77UL);
            var balance = new GameBalance();

            for (int i = 0; i < 30; i++)
            {
                Assert.That(second.NextColour(3, true), Is.EqualTo(first.NextColour(3, true)));
            }

            Assert.That(second.NextPressureRow(balance, 3),
                Is.EqualTo(first.NextPressureRow(balance, 3)));
        }

        [Test]
        public void GeneratorSnapshotResumesExactSequence()
        {
            var source = new RunContentGenerator(808UL);
            for (int i = 0; i < 11; i++)
            {
                source.NextColour(3, true);
            }

            ContentGeneratorSnapshot snapshot = source.CreateSnapshot();
            var restored = new RunContentGenerator(1UL);
            restored.Restore(snapshot);

            for (int i = 0; i < 40; i++)
            {
                Assert.That(restored.NextColour(5, true), Is.EqualTo(source.NextColour(5, true)));
            }
        }

        [Test]
        public void DuePressureAppliesOneDeterministicBottomRow()
        {
            GameSession session = CreateSession(90UL, ImmediatePressureBalance());
            SetEmptyBoard(session.Board);

            SessionActionResult result = Play(session, 0, 6);

            Assert.That(result.PressureRow, Is.Not.Null);
            Assert.That(result.PressureRow.Count, Is.EqualTo(BoardModel.Width));
            Assert.That(CountFilled(result.PressureRow), Is.InRange(3, 5));
            Assert.That(HasEvent(result, SessionEventType.PressureRowApplied), Is.True);
            Assert.That(session.State, Is.EqualTo(SessionState.Ready));
        }

        [Test]
        public void BlockedPressurePreservesSettledBoardAndEntersPendingLoss()
        {
            GameSession session = CreateSession(91UL, ImmediatePressureBalance());
            SetBoard(session.Board, Cell(0, 0, JellyColour.Coral));
            session.SelectRackSlot(0);
            session.TryBeginTurn(6);
            JellyColour[] settledBeforePressure = session.Board.CopyCells();

            SessionActionResult result = session.CompleteTurn();

            Assert.That(session.State, Is.EqualTo(SessionState.PendingLoss));
            Assert.That(session.Board.CopyCells(), Is.EqualTo(settledBeforePressure));
            Assert.That(result.PressureRow, Is.Null);
            Assert.That(HasEvent(result, SessionEventType.PressurePending), Is.True);
            Assert.That(session.ContinueCheckpoint, Is.Not.Null);
            Assert.That(session.ContinueCheckpoint.State, Is.EqualTo(SessionState.Ready));
            Assert.That(session.ContinueCheckpoint.Board.CopyCells(), Is.EqualTo(settledBeforePressure));
        }

        [Test]
        public void PressureCanCreateAndScoreAutomaticMatch()
        {
            bool found = false;
            for (ulong seed = 1; seed <= 100 && !found; seed++)
            {
                GameSession session = CreateSession(seed, ImmediatePressureBalance());
                SetBoard(session.Board,
                    Cell(0, 6, JellyColour.Coral), Cell(0, 7, JellyColour.Coral),
                    Cell(0, 8, JellyColour.Coral));
                SessionActionResult result = Play(session, 0, 6);
                if (result.PressureRow != null && result.PressureRow[0] == JellyColour.Coral)
                {
                    Assert.That(result.PressureResolution.WaveCount, Is.GreaterThanOrEqualTo(1));
                    Assert.That(result.PressureScoring.Waves[0].Points, Is.GreaterThanOrEqualTo(40L));
                    found = true;
                }
            }

            Assert.That(found, Is.True, "Expected a deterministic seed with Coral in pressure column zero.");
        }

        [Test]
        public void IdenticalSessionsProduceIdenticalPressureRowsAndBoards()
        {
            GameBalance balance = ImmediatePressureBalance();
            GameSession first = CreateSession(123UL, balance);
            GameSession second = CreateSession(123UL, balance);
            SetEmptyBoard(first.Board);
            SetEmptyBoard(second.Board);

            SessionActionResult firstResult = Play(first, 1, 3);
            SessionActionResult secondResult = Play(second, 1, 3);

            Assert.That(secondResult.PressureRow, Is.EqualTo(firstResult.PressureRow));
            Assert.That(second.Board.CopyCells(), Is.EqualTo(first.Board.CopyCells()));
            Assert.That(second.Score.CurrentScore, Is.EqualTo(first.Score.CurrentScore));
        }

        [Test]
        public void SettledAllFullBoardFinalisesGameOver()
        {
            var balance = new GameBalance(firstPressureTurn: 100,
                middlePressureTurn: 120, latePressureTurn: 150);
            GameSession session = CreateSession(700UL, balance);
            JellyColour[] cells = CheckerBoardWithTopGap();
            RestoreCells(session.Board, cells);
            session.SelectRackSlot(0);

            Assert.That(session.TryBeginTurn(0).Succeeded, Is.True);
            SessionActionResult result = session.CompleteTurn();

            Assert.That(session.State, Is.EqualTo(SessionState.GameOver));
            Assert.That(session.CurrentResult, Is.Not.Null);
            Assert.That(HasEvent(result, SessionEventType.NoLegalMove), Is.True);
        }

        [Test]
        public void ActiveColoursAdvanceAtConfiguredTurns()
        {
            var balance = new GameBalance(fourthColourTurn: 2, fifthColourTurn: 4,
                firstPressureTurn: 100, middlePressureTurn: 120, latePressureTurn: 150);
            GameSession session = CreateSession(42UL, balance);
            SetEmptyBoard(session.Board);

            Play(session, 0, 0);
            Assert.That(session.Board.ActiveColourCount, Is.EqualTo(3));
            Play(session, 1, 1);
            Assert.That(session.Board.ActiveColourCount, Is.EqualTo(4));
            Play(session, 2, 2);
            Play(session, 0, 3);
            Assert.That(session.Board.ActiveColourCount, Is.EqualTo(5));
        }

        private static GameBalance ImmediatePressureBalance() => new GameBalance(
            firstPressureTurn: 1, earlyPressureInterval: 6, middlePressureInterval: 5,
            latePressureInterval: 4, middlePressureTurn: 20, latePressureTurn: 45);

        private static GameSession CreateSession(ulong seed, GameBalance balance)
        {
            var session = new GameSession(balance);
            session.CompleteBoot();
            session.StartNewRun(seed);
            return session;
        }

        private static SessionActionResult Play(GameSession session, int slot, int column)
        {
            Assert.That(session.SelectRackSlot(slot).Succeeded, Is.True);
            Assert.That(session.TryBeginTurn(column).Succeeded, Is.True);
            return session.CompleteTurn();
        }

        private static bool HasEvent(SessionActionResult result, SessionEventType type)
        {
            for (int i = 0; i < result.Events.Count; i++)
            {
                if (result.Events[i].Type == type)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountFilled(System.Collections.Generic.IReadOnlyList<JellyColour> row)
        {
            int count = 0;
            for (int i = 0; i < row.Count; i++)
            {
                count += row[i] == JellyColour.Empty ? 0 : 1;
            }

            return count;
        }

        private static void SetEmptyBoard(BoardModel board) =>
            RestoreCells(board, new JellyColour[BoardModel.CellCount]);

        private static void SetBoard(BoardModel board, params TestCell[] testCells)
        {
            var cells = new JellyColour[BoardModel.CellCount];
            for (int i = 0; i < testCells.Length; i++)
            {
                cells[(testCells[i].Row * BoardModel.Width) + testCells[i].Column] = testCells[i].Colour;
            }

            RestoreCells(board, cells);
        }

        private static void RestoreCells(BoardModel board, JellyColour[] cells) =>
            board.Restore(new RunSnapshot(board.Seed, board.TurnCount, board.ActiveColourCount,
                board.RandomState, cells));

        private static JellyColour[] CheckerBoardWithTopGap()
        {
            var cells = new JellyColour[BoardModel.CellCount];
            for (int row = 0; row < BoardModel.Height; row++)
            {
                for (int column = 0; column < BoardModel.Width; column++)
                {
                    cells[(row * BoardModel.Width) + column] =
                        (JellyColour)(1 + ((row + column) % 3));
                }
            }

            cells[0] = JellyColour.Empty;
            return cells;
        }

        private static TestCell Cell(int column, int row, JellyColour colour) =>
            new TestCell(column, row, colour);

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
