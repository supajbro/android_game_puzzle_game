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
                presenter.Present(begin);
                Assert.That(begin.Succeeded, Is.True);

                SessionActionResult rapidDuplicate = session.TryBeginTurn(column);
                presenter.Present(rapidDuplicate);
                Assert.That(rapidDuplicate.Succeeded, Is.False);

                presenter.Present(session.CompleteTurn());
                if (session.State == SessionState.PendingLoss)
                {
                    presenter.Present(session.ConfirmGameOver());
                }

                Assert.That(presenter.QueuedActionCount, Is.Zero);
                Assert.That(presenter.RenderedBoardMatchesModel(), Is.True, $"Visual mismatch after move {move + 1}.");
            }

            yield return null;
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
