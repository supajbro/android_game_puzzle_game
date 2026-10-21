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
                Sprite sprite = ui.GetJellySettledFrame(colour);
                Assert.That(sprite, Is.Not.Null, $"{colour} is missing its expressive settled frame.");
                StringAssert.Contains("_Idle_", sprite.name);
                distinctSprites.Add(sprite);
                foreach (JellyAnimationType type in System.Enum.GetValues(typeof(JellyAnimationType)))
                {
                    Sprite[] frames = ui.GetJellyAnimationFrames(colour, type);
                    Assert.That(frames, Has.Length.EqualTo(10), $"{colour} {type} must contain ten frames.");
                    for (int frame = 0; frame < frames.Length; frame++)
                    {
                        Assert.That(frames[frame], Is.Not.Null);
                        Assert.That(frames[frame].rect.size, Is.EqualTo(new Vector2(256f, 256f)));
                        Assert.That(frames[frame].pivot, Is.EqualTo(new Vector2(128f, 128f)));
                    }
                }
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
            Assert.That(Object.FindObjectsByType<AnimatedJellyView>(FindObjectsSortMode.None).Length,
                Is.EqualTo(BoardModel.CellCount + GameSession.RackSize));
            Assert.That(GameObject.Find("Rack Jelly 0").GetComponent<AnimatedJellyView>(), Is.Not.Null);
            AssertCentredStableVisual("Jelly 0,0", "Cell 0,0", 82f, 0.92f);
            AssertCentredStableVisual("Jelly 6,8", "Cell 6,8", 82f, 0.92f);
            AssertCentredStableVisual("Rack Jelly 0", "Rack Hole 0", 165f, 0.96f);
            AssertCentredStableVisual("Rack Jelly 2", "Rack Hole 2", 165f, 0.96f);
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
        public IEnumerator JellyRootsRemainCentredAtSupportedPortraitResolutions()
        {
            int originalWidth = Screen.width;
            int originalHeight = Screen.height;
            var resolutions = new[]
            {
                new Vector2Int(360, 800), new Vector2Int(1080, 1920), new Vector2Int(1080, 2400)
            };

            SceneManager.LoadScene("Main");
            yield return null;
            GameObject.Find("Play Button").GetComponent<Button>().onClick.Invoke();
            yield return null;

            foreach (Vector2Int resolution in resolutions)
            {
                Screen.SetResolution(resolution.x, resolution.y, false);
                yield return null;
                Canvas.ForceUpdateCanvases();
                AssertWorldCentresMatch("Jelly 0,0", "Cell 0,0", resolution);
                AssertWorldCentresMatch("Jelly 3,4", "Cell 3,4", resolution);
                AssertWorldCentresMatch("Jelly 6,8", "Cell 6,8", resolution);
                AssertWorldCentresMatch("Rack Jelly 0", "Rack Hole 0", resolution);
                AssertWorldCentresMatch("Rack Jelly 2", "Rack Hole 2", resolution);
            }

            Screen.SetResolution(originalWidth, originalHeight, false);
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

        private static void AssertCentredStableVisual(string rootName, string holeName, float expectedRootSize,
            float expectedFill)
        {
            GameObject rootObject = GameObject.Find(rootName);
            GameObject holeObject = GameObject.Find(holeName);
            Assert.That(rootObject, Is.Not.Null);
            Assert.That(holeObject, Is.Not.Null);
            RectTransform root = rootObject.GetComponent<RectTransform>();
            RectTransform hole = holeObject.GetComponent<RectTransform>();
            RectTransform artwork = root.Find("Artwork") as RectTransform;
            Assert.That(artwork, Is.Not.Null);
            Assert.That(root.sizeDelta, Is.EqualTo(new Vector2(expectedRootSize, expectedRootSize)));
            Assert.That(root.anchoredPosition, Is.EqualTo(hole.anchoredPosition),
                "The stable jelly root must share the cell or rack-hole centre exactly.");
            Assert.That(artwork.anchoredPosition.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(Mathf.Abs(artwork.anchoredPosition.y), Is.LessThanOrEqualTo(3f),
                "Only the visual child may make a subtle idle movement around the centred root.");
            Assert.That(artwork.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(artwork.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(artwork.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(artwork.sizeDelta.x / expectedRootSize, Is.EqualTo(expectedFill).Within(0.001f));
            Assert.That(artwork.GetComponent<Image>().preserveAspect, Is.True);
        }

        private static void AssertWorldCentresMatch(string rootName, string holeName, Vector2Int resolution)
        {
            RectTransform root = GameObject.Find(rootName).GetComponent<RectTransform>();
            RectTransform hole = GameObject.Find(holeName).GetComponent<RectTransform>();
            Vector3 rootCentre = root.TransformPoint(root.rect.center);
            Vector3 holeCentre = hole.TransformPoint(hole.rect.center);
            Assert.That(Vector3.Distance(rootCentre, holeCentre), Is.LessThan(0.01f),
                $"{rootName} shifted away from {holeName} at {resolution.x} x {resolution.y}.");
        }
    }
}
