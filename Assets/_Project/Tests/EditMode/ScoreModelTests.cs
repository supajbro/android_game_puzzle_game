using System;
using JellyRiot.Core;
using NUnit.Framework;

namespace JellyRiot.Tests.EditMode
{
    public sealed class ScoreModelTests
    {
        [Test]
        public void FourPopOnFirstWaveAwardsForty()
        {
            BoardResolutionResult resolution = CreateBoard(
                Cell(0, 8, JellyColour.Coral), Cell(1, 8, JellyColour.Coral),
                Cell(2, 8, JellyColour.Coral), Cell(3, 8, JellyColour.Coral)).ResolveMatches();
            var score = new ScoreTracker();
            score.StartRun(GameMode.Endless);

            ScoreResolutionResult result = score.ApplyResolution(resolution);

            Assert.That(result.Waves[0].Points, Is.EqualTo(40L));
            Assert.That(score.CurrentScore, Is.EqualTo(40L));
        }

        [Test]
        public void FivePopOnSecondWaveAwardsOneHundred()
        {
            BoardResolutionResult resolution = CreateTwoWaveBoardWithFiveSecond().ResolveMatches();
            var score = new ScoreTracker();
            score.StartRun(GameMode.Endless);

            ScoreResolutionResult result = score.ApplyResolution(resolution);

            Assert.That(result.Waves.Count, Is.EqualTo(2));
            Assert.That(result.Waves[0].Points, Is.EqualTo(40L));
            Assert.That(result.Waves[1].ClearedCount, Is.EqualTo(5));
            Assert.That(result.Waves[1].Points, Is.EqualTo(100L));
            Assert.That(score.CurrentScore, Is.EqualTo(140L));
        }

        [Test]
        public void SimultaneousGroupsScoreCombinedCellsWithoutLargeGroupCue()
        {
            BoardResolutionResult resolution = CreateBoard(
                Cell(0, 8, JellyColour.Coral), Cell(1, 8, JellyColour.Coral),
                Cell(2, 8, JellyColour.Coral), Cell(3, 8, JellyColour.Coral),
                Cell(6, 5, JellyColour.Lemon), Cell(6, 6, JellyColour.Lemon),
                Cell(6, 7, JellyColour.Lemon), Cell(6, 8, JellyColour.Lemon)).ResolveMatches();
            var score = new ScoreTracker();
            score.StartRun(GameMode.Endless);

            ScoreResolutionResult result = score.ApplyResolution(resolution);

            Assert.That(result.Waves[0].Points, Is.EqualTo(80L));
            Assert.That(resolution.Waves[0].GroupSizes, Is.EqualTo(new[] { 4, 4 }));
            Assert.That(HasCue(result, PresentationCue.LargeGroup8Plus), Is.False);
        }

        [Test]
        public void MultipleWavesUpdateLongestChainAndChainMetadata()
        {
            var score = new ScoreTracker();
            score.StartRun(GameMode.Endless);

            ScoreResolutionResult result = score.ApplyResolution(
                CreateTwoWaveBoardWithFiveSecond().ResolveMatches());

            Assert.That(score.LongestChain, Is.EqualTo(2));
            Assert.That(HasCue(result, PresentationCue.Chain2), Is.True);
            Assert.That(HasCue(result, PresentationCue.Riot3Plus), Is.False);
        }

        [Test]
        public void ThirdWaveEmitsRiotMetadata()
        {
            var score = new ScoreTracker();
            score.StartRun(GameMode.Endless);

            ScoreResolutionResult result = score.ApplyResolution(CreateThreeWaveBoard().ResolveMatches());

            Assert.That(result.Waves.Count, Is.EqualTo(3));
            Assert.That(score.CurrentScore, Is.EqualTo(240L));
            Assert.That(score.LongestChain, Is.EqualTo(3));
            Assert.That(HasCue(result, PresentationCue.Chain2), Is.True);
            Assert.That(HasCue(result, PresentationCue.Riot3Plus), Is.True);
        }

        [Test]
        public void ActualEightCellComponentEmitsLargeGroupMetadata()
        {
            BoardModel board = CreateBoard(
                Cell(0, 8, JellyColour.Aqua), Cell(1, 8, JellyColour.Aqua),
                Cell(2, 8, JellyColour.Aqua), Cell(3, 8, JellyColour.Aqua),
                Cell(4, 8, JellyColour.Aqua), Cell(5, 8, JellyColour.Aqua),
                Cell(6, 8, JellyColour.Aqua), Cell(0, 7, JellyColour.Aqua));
            var score = new ScoreTracker();
            score.StartRun(GameMode.Endless);

            ScoreResolutionResult result = score.ApplyResolution(board.ResolveMatches());

            Assert.That(result.Waves[0].Points, Is.EqualTo(80L));
            Assert.That(HasCue(result, PresentationCue.LargeGroup8Plus), Is.True);
        }

        [Test]
        public void WavePointCalculationSaturatesInsteadOfOverflowing()
        {
            Assert.That(ScoreCalculator.CalculateWavePoints(int.MaxValue, int.MaxValue),
                Is.EqualTo(long.MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ScoreCalculator.CalculateWavePoints(-1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ScoreCalculator.CalculateWavePoints(4, 0));
        }

        [Test]
        public void NewRunResetsCurrentScoreAndLongestChainButKeepsBest()
        {
            var score = new ScoreTracker();
            score.StartRun(GameMode.Endless);
            score.ApplyResolution(CreateTwoWaveBoardWithFiveSecond().ResolveMatches());
            score.FinaliseRun();

            score.StartRun(GameMode.Endless);

            Assert.That(score.CurrentScore, Is.Zero);
            Assert.That(score.LongestChain, Is.Zero);
            Assert.That(score.BestEndlessScore, Is.EqualTo(140L));
        }

        [Test]
        public void DailyAndEndlessResultsRemainIsolated()
        {
            var score = new ScoreTracker();
            score.StartRun(GameMode.Endless);
            score.ApplyResolution(CreateFourBoard().ResolveMatches());
            RunScoreResult endless = score.FinaliseRun();

            score.StartRun(GameMode.Daily, "2026-10-17");
            score.ApplyResolution(CreateLargeGroupBoard().ResolveMatches());
            RunScoreResult daily = score.FinaliseRun();

            Assert.That(endless.FinalScore, Is.EqualTo(40L));
            Assert.That(daily.FinalScore, Is.EqualTo(80L));
            Assert.That(score.BestEndlessScore, Is.EqualTo(40L));
            Assert.That(score.GetDailyBest("2026-10-17"), Is.EqualTo(80L));
            Assert.That(score.GetDailyBest("2026-10-18"), Is.Zero);
        }

        [Test]
        public void PersonalBestMetadataIsProducedOnlyWhenRelevantRecordImproves()
        {
            var score = new ScoreTracker();
            score.StartRun(GameMode.Endless);
            score.ApplyResolution(CreateFourBoard().ResolveMatches());
            RunScoreResult first = score.FinaliseRun();
            score.StartRun(GameMode.Endless);
            score.ApplyResolution(CreateFourBoard().ResolveMatches());
            RunScoreResult equal = score.FinaliseRun();

            Assert.That(first.IsPersonalBest, Is.True);
            Assert.That(first.Presentation[0].Cue, Is.EqualTo(PresentationCue.PersonalBest));
            Assert.That(equal.IsPersonalBest, Is.False);
            Assert.That(equal.Presentation, Is.Empty);
        }

        [Test]
        public void SessionFinalisesScoreBeforeEnteringGameOver()
        {
            var session = new GameSession();
            session.CompleteBoot();
            session.StartNewRun(44UL);
            JellyColour colour = session.Rack[0];
            SetBoard(session.Board,
                Cell(0, 8, colour), Cell(1, 8, colour), Cell(2, 8, colour));
            session.SelectRackSlot(0);
            session.TryBeginTurn(3);
            session.CompleteTurn();

            SessionActionResult gameOver = session.ConfirmGameOver();

            Assert.That(session.CurrentResult.FinalScore, Is.EqualTo(40L));
            Assert.That(gameOver.Events[0].Type, Is.EqualTo(SessionEventType.RunFinalised));
            Assert.That(gameOver.Events[gameOver.Events.Count - 1].Type,
                Is.EqualTo(SessionEventType.StateChanged));
            Assert.That(session.State, Is.EqualTo(SessionState.GameOver));
        }

        [Test]
        public void SessionScoreEventsPrecedeRackRefillAndReadyState()
        {
            var session = new GameSession();
            session.CompleteBoot();
            session.StartNewRun(55UL);
            JellyColour colour = session.Rack[1];
            SetBoard(session.Board,
                Cell(0, 8, colour), Cell(1, 8, colour), Cell(2, 8, colour));
            session.SelectRackSlot(1);
            session.TryBeginTurn(3);

            SessionActionResult result = session.CompleteTurn();

            Assert.That(result.Events.Count, Is.EqualTo(5));
            Assert.That(result.Events[0].Type, Is.EqualTo(SessionEventType.WaveResolved));
            Assert.That(result.Events[1].Type, Is.EqualTo(SessionEventType.ScoreChanged));
            Assert.That(result.Events[1].Score, Is.EqualTo(40L));
            Assert.That(result.Events[2].Type, Is.EqualTo(SessionEventType.RackSlotRefilled));
            Assert.That(result.Events[3].Type, Is.EqualTo(SessionEventType.TurnCompleted));
            Assert.That(result.Events[4].Type, Is.EqualTo(SessionEventType.StateChanged));
            Assert.That(result.Events[4].State, Is.EqualTo(SessionState.Ready));
        }

        private static BoardModel CreateFourBoard() => CreateBoard(
            Cell(0, 8, JellyColour.Coral), Cell(1, 8, JellyColour.Coral),
            Cell(2, 8, JellyColour.Coral), Cell(3, 8, JellyColour.Coral));

        private static BoardModel CreateLargeGroupBoard() => CreateBoard(
            Cell(0, 8, JellyColour.Aqua), Cell(1, 8, JellyColour.Aqua),
            Cell(2, 8, JellyColour.Aqua), Cell(3, 8, JellyColour.Aqua),
            Cell(4, 8, JellyColour.Aqua), Cell(5, 8, JellyColour.Aqua),
            Cell(6, 8, JellyColour.Aqua), Cell(0, 7, JellyColour.Aqua));

        private static BoardModel CreateTwoWaveBoardWithFiveSecond() => CreateBoard(
            Cell(0, 8, JellyColour.Coral), Cell(1, 8, JellyColour.Coral),
            Cell(2, 8, JellyColour.Coral), Cell(3, 8, JellyColour.Coral),
            Cell(0, 7, JellyColour.Lemon), Cell(1, 6, JellyColour.Lemon),
            Cell(2, 5, JellyColour.Lemon), Cell(3, 4, JellyColour.Lemon),
            Cell(4, 8, JellyColour.Lemon));

        private static BoardModel CreateThreeWaveBoard() => CreateBoard(
            Cell(6, 1, JellyColour.Lemon),
            Cell(6, 2, JellyColour.Aqua),
            Cell(0, 3, JellyColour.Lemon), Cell(6, 3, JellyColour.Coral),
            Cell(0, 4, JellyColour.Coral), Cell(2, 4, JellyColour.Aqua),
            Cell(5, 4, JellyColour.Aqua), Cell(6, 4, JellyColour.Coral),
            Cell(0, 5, JellyColour.Coral), Cell(2, 5, JellyColour.Lemon),
            Cell(4, 5, JellyColour.Aqua), Cell(5, 5, JellyColour.Lemon),
            Cell(6, 5, JellyColour.Coral), Cell(4, 6, JellyColour.Coral),
            Cell(5, 6, JellyColour.Aqua), Cell(6, 6, JellyColour.Lemon),
            Cell(0, 7, JellyColour.Lemon), Cell(1, 7, JellyColour.Coral),
            Cell(2, 7, JellyColour.Aqua), Cell(3, 7, JellyColour.Coral),
            Cell(4, 7, JellyColour.Coral), Cell(5, 7, JellyColour.Coral),
            Cell(6, 7, JellyColour.Lemon), Cell(0, 8, JellyColour.Aqua),
            Cell(2, 8, JellyColour.Aqua), Cell(4, 8, JellyColour.Aqua),
            Cell(6, 8, JellyColour.Lemon));

        private static BoardModel CreateBoard(params TestCell[] testCells)
        {
            var board = new BoardModel(4004UL);
            SetBoard(board, testCells);
            return board;
        }

        private static void SetBoard(BoardModel board, params TestCell[] testCells)
        {
            var cells = new JellyColour[BoardModel.CellCount];
            for (int i = 0; i < testCells.Length; i++)
            {
                TestCell cell = testCells[i];
                cells[(cell.Row * BoardModel.Width) + cell.Column] = cell.Colour;
            }

            board.Restore(new RunSnapshot(board.Seed, 0, board.ActiveColourCount,
                board.RandomState, cells));
        }

        private static bool HasCue(ScoreResolutionResult result, PresentationCue cue)
        {
            for (int i = 0; i < result.Presentation.Count; i++)
            {
                if (result.Presentation[i].Cue == cue)
                {
                    return true;
                }
            }

            return false;
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
