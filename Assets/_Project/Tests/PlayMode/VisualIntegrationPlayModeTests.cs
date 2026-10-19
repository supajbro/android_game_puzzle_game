using System.Collections;
using System.Collections.Generic;
using JellyRiot.Core;
using JellyRiot.UI;
using JellyRiot.Visuals;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace JellyRiot.Tests.PlayMode
{
    public sealed class VisualIntegrationPlayModeTests
    {
        [UnityTest]
        public IEnumerator MainSceneBuildsCompleteBoardRackAndDistinctColourMapping()
        {
            SceneManager.LoadScene("Main");
            yield return null;

            JellyRiotMainUI ui = Object.FindFirstObjectByType<JellyRiotMainUI>();
            Assert.That(ui, Is.Not.Null);
            Assert.That(ui.Session.State, Is.EqualTo(SessionState.Home));
            Assert.That(ui.BoardCellCount, Is.EqualTo(BoardModel.CellCount));
            Assert.That(ui.RackSlotCount, Is.EqualTo(GameSession.RackSize));

            var distinctSprites = new HashSet<Sprite>();
            for (JellyColour colour = JellyColour.Coral; colour <= JellyColour.Mint; colour++)
            {
                Sprite sprite = ui.GetJellySprite(colour);
                Assert.That(sprite, Is.Not.Null, $"{colour} is missing its supplied sprite.");
                distinctSprites.Add(sprite);
            }
            Assert.That(distinctSprites.Count, Is.EqualTo(5));

            GameObject playObject = GameObject.Find("Play Button");
            Assert.That(playObject, Is.Not.Null);
            Assert.That(playObject.GetComponent<Image>().raycastTarget, Is.True);
            playObject.GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(ui.Session.State, Is.EqualTo(SessionState.Ready));
            Assert.That(GameObject.Find("7 x 9 Grid"), Is.Not.Null);
            Assert.That(GameObject.Find("Rack Button 0"), Is.Not.Null);
            Assert.That(GameObject.Find("Rack Button 1"), Is.Not.Null);
            Assert.That(GameObject.Find("Rack Button 2"), Is.Not.Null);
            Assert.That(ui.GridPresenter, Is.Not.Null);
            Assert.That(ui.GridPresenter.HasCompleteAnimationLibrary(), Is.True);
            Assert.That(ui.GridPresenter.RenderedBoardMatchesModel(), Is.True);
        }

        [UnityTest]
        public IEnumerator PresenterRemainsSynchronisedAcrossOneHundredRapidMovesAndInterruptions()
        {
            SceneManager.LoadScene("Main");
            yield return null;
            JellyRiotMainUI ui = Object.FindFirstObjectByType<JellyRiotMainUI>();
            GameObject.Find("Play Button").GetComponent<Button>().onClick.Invoke();
            yield return null;

            GameSession session = ui.Session;
            GridViewPresenter presenter = ui.GridPresenter;
            for (int move = 0; move < 100; move++)
            {
                if (session.State == SessionState.GameOver)
                {
                    presenter.Present(session.Replay());
                }

                if (move > 0 && move % 23 == 0)
                {
                    presenter.Present(session.ResetRun());
                }

                if (move > 0 && move % 11 == 0)
                {
                    presenter.Present(session.Pause());
                    presenter.CancelPendingAndSynchronise();
                    presenter.Present(session.Resume());
                }

                presenter.Present(session.SelectRackSlot(move % GameSession.RackSize));
                int column = FirstLegalColumn(session.Board);
                SessionActionResult begin = session.TryBeginTurn(column);
                Assert.That(begin.Succeeded, Is.True);

                SessionActionResult rapidDuplicate = session.TryBeginTurn(column);
                Assert.That(rapidDuplicate.Succeeded, Is.False);

                SessionActionResult complete = session.CompleteTurn();
                SessionActionResult terminal = null;
                if (session.State == SessionState.PendingLoss)
                {
                    terminal = session.ConfirmGameOver();
                }

                presenter.PresentTurn(begin, complete, terminal);
                presenter.CancelPendingAndSynchronise();

                Assert.That(presenter.QueuedActionCount, Is.Zero);
                Assert.That(presenter.RenderedBoardMatchesModel(), Is.True, $"Visual mismatch after move {move + 1}.");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator ThirtyAnimatedTurnsSettleAndPauseFreezesActiveDrop()
        {
            SceneManager.LoadScene("Main");
            yield return null;
            JellyRiotMainUI ui = Object.FindFirstObjectByType<JellyRiotMainUI>();
            GameObject.Find("Play Button").GetComponent<Button>().onClick.Invoke();
            yield return null;

            GameSession session = ui.Session;
            GridViewPresenter presenter = ui.GridPresenter;
            Assert.That(presenter.HasCompleteAnimationLibrary(), Is.True);

            for (int move = 0; move < 30; move++)
            {
                if (session.State == SessionState.GameOver) presenter.Present(session.Replay());
                presenter.Present(session.SelectRackSlot(move % GameSession.RackSize));
                int column = FirstLegalColumn(session.Board);
                SessionActionResult begin = session.TryBeginTurn(column);
                SessionActionResult duplicate = session.TryBeginTurn(column);
                Assert.That(duplicate.Succeeded, Is.False);
                SessionActionResult complete = session.CompleteTurn();
                SessionActionResult terminal = session.State == SessionState.PendingLoss
                    ? session.ConfirmGameOver()
                    : null;
                presenter.PresentTurn(begin, complete, terminal);
                Assert.That(presenter.ActiveFallDuration, Is.InRange(0.14f, 0.24f));

                if (move == 4)
                {
                    presenter.Present(session.Pause());
                    yield return null;
                    yield return null;
                    Assert.That(presenter.IsPresenting, Is.True, "A paused drop must not complete behind the pause screen.");
                    presenter.Present(session.Resume());
                }

                float timeout = Time.realtimeSinceStartup + 1.5f;
                while (presenter.IsPresenting && Time.realtimeSinceStartup < timeout) yield return null;
                Assert.That(presenter.IsPresenting, Is.False, $"Animation did not settle after move {move + 1}.");
                Assert.That(presenter.RenderedBoardMatchesModel(), Is.True, $"Visual mismatch after animated move {move + 1}.");
            }
        }

        [UnityTest]
        public IEnumerator ReducedMotionUsesMinimalDropAndDevelopmentSlowMotionDoesNotChangeCore()
        {
            SceneManager.LoadScene("Main");
            yield return null;
            JellyRiotMainUI ui = Object.FindFirstObjectByType<JellyRiotMainUI>();
            GameObject.Find("Play Button").GetComponent<Button>().onClick.Invoke();
            yield return null;

            GameSession session = ui.Session;
            GridViewPresenter presenter = ui.GridPresenter;
            DeterministicRandomState randomBeforeVisualSettings = session.CreateSnapshot().Generator.RandomState;
            presenter.SetReducedMotion(true);
            presenter.ToggleSlowMotion();
            Assert.That(session.CreateSnapshot().Generator.RandomState, Is.EqualTo(randomBeforeVisualSettings),
                "Visual speed controls must not consume gameplay randomness.");
            presenter.Present(session.SelectRackSlot(0));
            SessionActionResult begin = session.TryBeginTurn(FirstLegalColumn(session.Board));
            SessionActionResult complete = session.CompleteTurn();
            presenter.PresentTurn(begin, complete);
            Assert.That(presenter.ActiveFallDuration, Is.EqualTo(0.08f).Within(0.001f));

            float timeout = Time.realtimeSinceStartup + 1.5f;
            while (presenter.IsPresenting && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(presenter.RenderedBoardMatchesModel(), Is.True);
        }

        private static int FirstLegalColumn(BoardModel board)
        {
            for (int column = 0; column < BoardModel.Width; column++)
            {
                if (!board.IsColumnFull(column)) return column;
            }
            Assert.Fail("Expected at least one legal column.");
            return -1;
        }
    }
}
