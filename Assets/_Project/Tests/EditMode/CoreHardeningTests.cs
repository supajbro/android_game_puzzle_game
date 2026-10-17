using System;
using JellyRiot.Core;
using NUnit.Framework;

namespace JellyRiot.Tests.EditMode
{
    public sealed class CoreHardeningTests
    {
        [Test]
        public void SessionSnapshotRestoresBoardRackScoreCountdownAndFutureOutcome()
        {
            GameSession first = CreateSession(707UL);
            PlayPolicyTurns(first, 5);
            SessionSnapshot snapshot = first.CreateSnapshot();
            var second = new GameSession();
            second.Restore(snapshot);

            AssertSessionsEqual(first, second);
            PlayOne(first, 1, FirstLegalColumn(first));
            PlayOne(second, 1, FirstLegalColumn(second));
            AssertSessionsEqual(first, second);
        }

        [Test]
        public void ResolvingSnapshotCompletesExactlyOnceAfterRestore()
        {
            GameSession first = CreateSession(708UL);
            first.SelectRackSlot(2);
            first.TryBeginTurn(3);
            SessionSnapshot snapshot = first.CreateSnapshot();
            var second = new GameSession();
            second.Restore(snapshot);

            first.CompleteTurn();
            second.CompleteTurn();

            AssertSessionsEqual(first, second);
            Assert.That(second.Board.TurnCount, Is.EqualTo(1));
        }

        [Test]
        public void PendingLossSnapshotRetainsContinueReadyCheckpoint()
        {
            var balance = new GameBalance(firstPressureTurn: 1);
            GameSession first = CreateSession(709UL, balance);
            JellyColour[] cells = new JellyColour[BoardModel.CellCount];
            cells[0] = JellyColour.Coral;
            RestoreCells(first.Board, cells);
            PlayOne(first, 0, 6);
            Assert.That(first.State, Is.EqualTo(SessionState.PendingLoss));

            SessionSnapshot snapshot = first.CreateSnapshot();
            var second = new GameSession(balance);
            second.Restore(snapshot);

            Assert.That(second.State, Is.EqualTo(SessionState.PendingLoss));
            Assert.That(second.ContinueCheckpoint, Is.Not.Null);
            Assert.That(second.ContinueCheckpoint.State, Is.EqualTo(SessionState.Ready));
            Assert.That(second.ContinueCheckpoint.Board.CopyCells(),
                Is.EqualTo(first.ContinueCheckpoint.Board.CopyCells()));
        }

        [Test]
        public void GameOverSnapshotRestoresFinalResult()
        {
            var balance = new GameBalance(firstPressureTurn: 1);
            GameSession first = CreateSession(710UL, balance);
            JellyColour[] cells = new JellyColour[BoardModel.CellCount];
            cells[0] = JellyColour.Coral;
            RestoreCells(first.Board, cells);
            PlayOne(first, 0, 6);
            first.ConfirmGameOver();

            var second = new GameSession(balance);
            second.Restore(first.CreateSnapshot());

            Assert.That(second.State, Is.EqualTo(SessionState.GameOver));
            Assert.That(second.CurrentResult, Is.Not.Null);
            Assert.That(second.CurrentResult.FinalScore, Is.EqualTo(first.CurrentResult.FinalScore));
            Assert.That(second.CurrentResult.LongestChain, Is.EqualTo(first.CurrentResult.LongestChain));
            Assert.That(second.CurrentResult.IsPersonalBest, Is.EqualTo(first.CurrentResult.IsPersonalBest));
        }

        [Test]
        public void DailySeedIsStableAndProducesIdenticalDailyRuns()
        {
            ulong firstSeed = DailySeed.Derive("2026-10-17", 1, "jelly-riot-v1");
            ulong secondSeed = DailySeed.Derive("2026-10-17", 1, "jelly-riot-v1");
            ulong nextDay = DailySeed.Derive("2026-10-18", 1, "jelly-riot-v1");
            GameSession first = CreateSession(firstSeed, GameBalance.Default,
                GameMode.Daily, "2026-10-17");
            GameSession second = CreateSession(secondSeed, GameBalance.Default,
                GameMode.Daily, "2026-10-17");

            Assert.That(secondSeed, Is.EqualTo(firstSeed));
            Assert.That(nextDay, Is.Not.EqualTo(firstSeed));
            AssertSessionsEqual(first, second);
        }

        [Test]
        public void FixedMoveListReplaysToEquivalentStateAfterEveryTurn()
        {
            int[] slots = { 0, 2, 1, 1, 0, 2, 0, 1, 2, 0 };
            int[] columns = { 0, 6, 3, 2, 4, 1, 5, 0, 6, 3 };
            GameSession first = CreateSession(991UL);
            GameSession second = CreateSession(991UL);

            for (int i = 0; i < slots.Length; i++)
            {
                if (first.State != SessionState.Ready)
                {
                    break;
                }

                PlayOne(first, slots[i], columns[i]);
                PlayOne(second, slots[i], columns[i]);
                AssertSessionsEqual(first, second);
            }
        }

        [Test]
        public void GoldenSeed991MatchesExpectedBoardAndScoreAfterEveryTurn()
        {
            int[] slots = { 0, 2, 1, 1, 0, 2, 0, 1 };
            int[] columns = { 0, 6, 3, 2, 4, 1, 5, 0 };
            long[] scores = { 0L, 0L, 50L, 50L, 50L, 90L, 90L, 90L };
            string[] boards =
            {
                "000000000000000000000000000000000002000300312023031211233123121",
                "000000000000000000000000000000000002000300312023131211233123121",
                "000000000000000000000000000000000002000000310033131011233103121",
                "000000000000000000000000000000000002000000310033131011233133121",
                "000000000000000000000000000000000002000200310033131011233133121",
                "000000000000000000000000000020002003000331300112330331211020030",
                "000000000000000000000000000020002203000331300112330331211020030",
                "000000000000000000000200000020002203000331300112330331211020030"
            };
            GameSession session = CreateSession(991UL);

            for (int turn = 0; turn < slots.Length; turn++)
            {
                PlayOne(session, slots[turn], columns[turn]);
                Assert.That(session.Score.CurrentScore, Is.EqualTo(scores[turn]),
                    $"Score mismatch after turn {turn + 1}.");
                Assert.That(BoardSignature(session.Board), Is.EqualTo(boards[turn]),
                    $"Board mismatch after turn {turn + 1}.");
            }
        }

        [Test]
        public void ThousandSeedBasicPolicyIsDeterministicAndTerminates()
        {
            const int seedCount = 1000;
            const int maximumTurns = 200;
            var turns = new int[seedCount];
            long totalTurns = 0L;
            long totalCascadeWaves = 0L;
            int earlyDeaths = 0;
            int cutoffCount = 0;

            for (int index = 0; index < seedCount; index++)
            {
                SimulationResult first = Simulate((ulong)index, maximumTurns);
                SimulationResult replay = Simulate((ulong)index, maximumTurns);
                Assert.That(replay.Turns, Is.EqualTo(first.Turns), $"Turn mismatch for seed {index}.");
                Assert.That(replay.Score, Is.EqualTo(first.Score), $"Score mismatch for seed {index}.");
                Assert.That(replay.BoardSignature, Is.EqualTo(first.BoardSignature),
                    $"Board mismatch for seed {index}.");
                turns[index] = first.Turns;
                totalTurns += first.Turns;
                totalCascadeWaves += first.CascadeWaves;
                earlyDeaths += first.Turns < 4 ? 1 : 0;
                cutoffCount += first.HitCutoff ? 1 : 0;
            }

            Array.Sort(turns);
            double mean = totalTurns / (double)seedCount;
            double median = (turns[499] + turns[500]) * 0.5;
            double cascadeRate = totalTurns == 0 ? 0.0 : totalCascadeWaves / (double)totalTurns;
            Console.WriteLine($"BALANCE mean={mean:F2} median={median:F1} earlyDeaths={earlyDeaths} " +
                              $"cascadeWavesPerTurn={cascadeRate:F4} cutoffs={cutoffCount}");

            Assert.That(cutoffCount, Is.Zero);
            Assert.That(earlyDeaths, Is.LessThan(seedCount / 2));
        }

        private static SimulationResult Simulate(ulong seed, int maximumTurns)
        {
            GameSession session = CreateSession(seed);
            var policy = new DeterministicRandom(seed ^ 0x504F4C494359UL);
            int cascadeWaves = 0;
            while (session.State == SessionState.Ready && session.Board.TurnCount < maximumTurns)
            {
                int[] legal = new int[BoardModel.Width];
                int legalCount = 0;
                for (int column = 0; column < BoardModel.Width; column++)
                {
                    if (!session.Board.IsColumnFull(column))
                    {
                        legal[legalCount++] = column;
                    }
                }

                if (legalCount == 0)
                {
                    session.ConfirmGameOver();
                    break;
                }

                int slot = policy.NextInt(GameSession.RackSize);
                int columnChoice = legal[policy.NextInt(legalCount)];
                session.SelectRackSlot(slot);
                session.TryBeginTurn(columnChoice);
                SessionActionResult result = session.CompleteTurn();
                cascadeWaves += result.Resolution.WaveCount;
                if (result.PressureResolution != null)
                {
                    cascadeWaves += result.PressureResolution.WaveCount;
                }

                if (session.State == SessionState.PendingLoss)
                {
                    session.ConfirmGameOver();
                }
            }

            bool cutoff = session.State == SessionState.Ready && session.Board.TurnCount >= maximumTurns;
            return new SimulationResult(session.Board.TurnCount, session.Score.CurrentScore,
                cascadeWaves, cutoff, BoardSignature(session.Board));
        }

        private static GameSession CreateSession(ulong seed, GameBalance balance = null,
            GameMode mode = GameMode.Endless, string dailyKey = null)
        {
            var session = new GameSession(balance ?? GameBalance.Default);
            session.CompleteBoot();
            session.StartNewRun(seed, mode, dailyKey);
            return session;
        }

        private static void PlayPolicyTurns(GameSession session, int count)
        {
            for (int turn = 0; turn < count && session.State == SessionState.Ready; turn++)
            {
                PlayOne(session, turn % GameSession.RackSize, FirstLegalColumn(session));
            }
        }

        private static void PlayOne(GameSession session, int slot, int column)
        {
            Assert.That(session.SelectRackSlot(slot).Succeeded, Is.True);
            Assert.That(session.TryBeginTurn(column).Succeeded, Is.True);
            Assert.That(session.CompleteTurn().Succeeded, Is.True);
        }

        private static int FirstLegalColumn(GameSession session)
        {
            for (int column = 0; column < BoardModel.Width; column++)
            {
                if (!session.Board.IsColumnFull(column))
                {
                    return column;
                }
            }

            return -1;
        }

        private static void AssertSessionsEqual(GameSession expected, GameSession actual)
        {
            Assert.That(actual.State, Is.EqualTo(expected.State));
            Assert.That(actual.Board.CopyCells(), Is.EqualTo(expected.Board.CopyCells()));
            Assert.That(actual.Board.TurnCount, Is.EqualTo(expected.Board.TurnCount));
            Assert.That(actual.Rack, Is.EqualTo(expected.Rack));
            Assert.That(actual.SelectedSlot, Is.EqualTo(expected.SelectedSlot));
            Assert.That(actual.Score.CurrentScore, Is.EqualTo(expected.Score.CurrentScore));
            Assert.That(actual.Score.BestEndlessScore, Is.EqualTo(expected.Score.BestEndlessScore));
            Assert.That(actual.NextRowIn, Is.EqualTo(expected.NextRowIn));
        }

        private static void RestoreCells(BoardModel board, JellyColour[] cells) =>
            board.Restore(new RunSnapshot(board.Seed, board.TurnCount, board.ActiveColourCount,
                board.RandomState, cells));

        private static string BoardSignature(BoardModel board)
        {
            char[] signature = new char[BoardModel.CellCount];
            for (int i = 0; i < signature.Length; i++)
            {
                signature[i] = (char)('0' + (int)board.Cells[i]);
            }

            return new string(signature);
        }

        private readonly struct SimulationResult
        {
            public SimulationResult(int turns, long score, int cascadeWaves, bool hitCutoff,
                string boardSignature)
            {
                Turns = turns;
                Score = score;
                CascadeWaves = cascadeWaves;
                HitCutoff = hitCutoff;
                BoardSignature = boardSignature;
            }

            public int Turns { get; }
            public long Score { get; }
            public int CascadeWaves { get; }
            public bool HitCutoff { get; }
            public string BoardSignature { get; }
        }
    }
}
