using System;
using System.Collections.Generic;
using JellyRiot.Core;
using NUnit.Framework;

namespace JellyRiot.Tests.EditMode
{
    public sealed class GameSessionTests
    {
        [Test]
        public void BootHomeAndNewRunTransitionsAreExplicit()
        {
            var session = new GameSession();

            Assert.That(session.State, Is.EqualTo(SessionState.Boot));
            Assert.That(session.StartNewRun(3UL).Failure, Is.EqualTo(SessionFailure.InvalidState));
            Assert.That(session.CompleteBoot().Succeeded, Is.True);
            Assert.That(session.State, Is.EqualTo(SessionState.Home));
            Assert.That(session.StartNewRun(3UL).Succeeded, Is.True);
            Assert.That(session.State, Is.EqualTo(SessionState.Ready));
        }

        [Test]
        public void NewRunHasThreeVisibleCandidates()
        {
            GameSession session = CreateReadySession();

            Assert.That(session.Rack.Count, Is.EqualTo(3));
            for (int slot = 0; slot < GameSession.RackSize; slot++)
            {
                Assert.That(session.Rack[slot], Is.Not.EqualTo(JellyColour.Empty));
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void ValidTurnConsumesAndRefillsOnlySelectedSlot(int selectedSlot)
        {
            GameSession session = CreateReadySession();
            JellyColour[] before = CopyRack(session);

            session.SelectRackSlot(selectedSlot);
            SessionActionResult begin = session.TryBeginTurn(0);

            Assert.That(begin.Succeeded, Is.True);
            Assert.That(session.Rack[selectedSlot], Is.EqualTo(JellyColour.Empty));
            for (int slot = 0; slot < GameSession.RackSize; slot++)
            {
                if (slot != selectedSlot)
                {
                    Assert.That(session.Rack[slot], Is.EqualTo(before[slot]));
                }
            }

            SessionActionResult complete = session.CompleteTurn();
            Assert.That(complete.Succeeded, Is.True);
            Assert.That(session.Rack[selectedSlot], Is.Not.EqualTo(JellyColour.Empty));
            for (int slot = 0; slot < GameSession.RackSize; slot++)
            {
                if (slot != selectedSlot)
                {
                    Assert.That(session.Rack[slot], Is.EqualTo(before[slot]));
                }
            }
        }

        [TestCase(-1)]
        [TestCase(3)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void IllegalSlotDoesNotReplaceExistingSelection(int illegalSlot)
        {
            GameSession session = CreateReadySession();
            session.SelectRackSlot(1);

            SessionActionResult result = session.SelectRackSlot(illegalSlot);

            Assert.That(result.Failure, Is.EqualTo(SessionFailure.InvalidSlot));
            Assert.That(session.SelectedSlot, Is.EqualTo(1));
        }

        [Test]
        public void InvalidColumnKeepsSelectedPieceAndRack()
        {
            GameSession session = CreateReadySession();
            session.SelectRackSlot(2);
            JellyColour[] before = CopyRack(session);
            int turnsBefore = session.Board.TurnCount;

            SessionActionResult result = session.TryBeginTurn(-1);

            Assert.That(result.Failure, Is.EqualTo(SessionFailure.InvalidColumn));
            Assert.That(session.SelectedSlot, Is.EqualTo(2));
            Assert.That(session.Rack, Is.EqualTo(before));
            Assert.That(session.Board.TurnCount, Is.EqualTo(turnsBefore));
            Assert.That(session.State, Is.EqualTo(SessionState.Ready));
        }

        [Test]
        public void FullColumnKeepsSelectedPieceAndRack()
        {
            GameSession session = CreateReadySession();
            SetColumnFull(session.Board, 4);
            session.SelectRackSlot(0);
            JellyColour[] before = CopyRack(session);

            SessionActionResult result = session.TryBeginTurn(4);

            Assert.That(result.Failure, Is.EqualTo(SessionFailure.ColumnFull));
            Assert.That(session.SelectedSlot, Is.Zero);
            Assert.That(session.Rack, Is.EqualTo(before));
            Assert.That(session.State, Is.EqualTo(SessionState.Ready));
        }

        [Test]
        public void RapidDuplicateTurnCannotPlaceTwice()
        {
            GameSession session = CreateReadySession();
            session.SelectRackSlot(0);

            SessionActionResult first = session.TryBeginTurn(1);
            int turnsAfterFirst = session.Board.TurnCount;
            JellyColour[] cellsAfterFirst = session.Board.CopyCells();
            SessionActionResult duplicate = session.TryBeginTurn(2);

            Assert.That(first.Succeeded, Is.True);
            Assert.That(duplicate.Failure, Is.EqualTo(SessionFailure.InvalidState));
            Assert.That(session.Board.TurnCount, Is.EqualTo(turnsAfterFirst));
            Assert.That(session.Board.CopyCells(), Is.EqualTo(cellsAfterFirst));
            Assert.That(session.State, Is.EqualTo(SessionState.Resolving));
        }

        [Test]
        public void SelectionAndPauseInputsAreRejectedWhileResolving()
        {
            GameSession session = CreateReadySession();
            session.SelectRackSlot(0);
            session.TryBeginTurn(0);

            Assert.That(session.SelectRackSlot(1).Failure, Is.EqualTo(SessionFailure.InvalidState));
            Assert.That(session.Pause().Failure, Is.EqualTo(SessionFailure.InvalidState));
        }

        [Test]
        public void BeginTurnEventsHaveStableSemanticOrder()
        {
            GameSession session = CreateReadySession();
            session.SelectRackSlot(1);

            SessionActionResult result = session.TryBeginTurn(2);

            Assert.That(EventTypes(result), Is.EqualTo(new[]
            {
                SessionEventType.JellyPlaced,
                SessionEventType.RackSlotConsumed,
                SessionEventType.StateChanged
            }));
            Assert.That(result.Events[2].PreviousState, Is.EqualTo(SessionState.Ready));
            Assert.That(result.Events[2].State, Is.EqualTo(SessionState.Resolving));
        }

        [Test]
        public void EmptyResolutionRefillsBeforeTurnCompletesAndReturnsReady()
        {
            GameSession session = CreateReadySession();
            SetEmptyBoard(session.Board);
            session.SelectRackSlot(0);
            session.TryBeginTurn(3);

            SessionActionResult result = session.CompleteTurn();

            Assert.That(EventTypes(result), Is.EqualTo(new[]
            {
                SessionEventType.RackSlotRefilled,
                SessionEventType.TurnCompleted,
                SessionEventType.StateChanged
            }));
            Assert.That(session.State, Is.EqualTo(SessionState.Ready));
            Assert.That(result.Resolution.WaveCount, Is.Zero);
        }

        [Test]
        public void PauseAndResumePreserveSelection()
        {
            GameSession session = CreateReadySession();
            session.SelectRackSlot(2);

            Assert.That(session.Pause().Succeeded, Is.True);
            Assert.That(session.State, Is.EqualTo(SessionState.Paused));
            Assert.That(session.SelectedSlot, Is.EqualTo(2));
            Assert.That(session.Resume().Succeeded, Is.True);
            Assert.That(session.State, Is.EqualTo(SessionState.Ready));
            Assert.That(session.SelectedSlot, Is.EqualTo(2));
        }

        [Test]
        public void PendingLossCanFinaliseBeforeGameOverStateEvent()
        {
            GameSession session = CreateReadySession();
            Assert.That(session.EnterPendingLoss().Succeeded, Is.True);

            SessionActionResult result = session.ConfirmGameOver();

            Assert.That(session.CurrentResult, Is.Not.Null);
            Assert.That(result.Events[0].Type, Is.EqualTo(SessionEventType.RunFinalised));
            Assert.That(result.Events[result.Events.Count - 1].Type, Is.EqualTo(SessionEventType.StateChanged));
            Assert.That(result.Events[result.Events.Count - 1].State, Is.EqualTo(SessionState.GameOver));
        }

        [Test]
        public void ReplayRestoresOriginalBoardRackAndScore()
        {
            GameSession session = CreateReadySession(9876UL);
            JellyColour[] initialBoard = session.Board.CopyCells();
            JellyColour[] initialRack = CopyRack(session);
            session.SelectRackSlot(0);
            session.TryBeginTurn(0);
            session.CompleteTurn();
            session.ConfirmGameOver();

            SessionActionResult replay = session.Replay();

            Assert.That(replay.Succeeded, Is.True);
            Assert.That(session.Board.CopyCells(), Is.EqualTo(initialBoard));
            Assert.That(session.Rack, Is.EqualTo(initialRack));
            Assert.That(session.Score.CurrentScore, Is.Zero);
            Assert.That(session.SelectedSlot, Is.EqualTo(-1));
            Assert.That(session.State, Is.EqualTo(SessionState.Ready));
        }

        [Test]
        public void ResetRestoresSameDeterministicRunFromPausedState()
        {
            GameSession session = CreateReadySession(4321UL);
            JellyColour[] initialBoard = session.Board.CopyCells();
            JellyColour[] initialRack = CopyRack(session);
            session.SelectRackSlot(1);
            session.Pause();

            SessionActionResult reset = session.ResetRun();

            Assert.That(reset.Succeeded, Is.True);
            Assert.That(session.Board.CopyCells(), Is.EqualTo(initialBoard));
            Assert.That(session.Rack, Is.EqualTo(initialRack));
            Assert.That(session.SelectedSlot, Is.EqualTo(-1));
            Assert.That(session.State, Is.EqualTo(SessionState.Ready));
        }

        [Test]
        public void ActionEventCollectionsAreReadOnly()
        {
            GameSession session = CreateReadySession();
            SessionActionResult result = session.SelectRackSlot(0);

            Assert.Throws<NotSupportedException>(() => ((IList<SessionEvent>)result.Events).Clear());
        }

        private static GameSession CreateReadySession(ulong seed = 3003UL)
        {
            var session = new GameSession();
            session.CompleteBoot();
            session.StartNewRun(seed);
            return session;
        }

        private static JellyColour[] CopyRack(GameSession session)
        {
            var copy = new JellyColour[GameSession.RackSize];
            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = session.Rack[i];
            }

            return copy;
        }

        private static SessionEventType[] EventTypes(SessionActionResult result)
        {
            var types = new SessionEventType[result.Events.Count];
            for (int i = 0; i < types.Length; i++)
            {
                types[i] = result.Events[i].Type;
            }

            return types;
        }

        private static void SetEmptyBoard(BoardModel board)
        {
            board.Restore(new RunSnapshot(board.Seed, board.TurnCount, board.ActiveColourCount,
                board.RandomState, new JellyColour[BoardModel.CellCount]));
        }

        private static void SetColumnFull(BoardModel board, int column)
        {
            JellyColour[] cells = board.CopyCells();
            for (int row = 0; row < BoardModel.Height; row++)
            {
                cells[(row * BoardModel.Width) + column] = (JellyColour)(1 + (row % 3));
            }

            board.Restore(new RunSnapshot(board.Seed, board.TurnCount, board.ActiveColourCount,
                board.RandomState, cells));
        }
    }
}
