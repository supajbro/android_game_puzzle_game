using System.Collections;
using System.Collections.Generic;
using JellyRiot.Core;
using JellyRiot.UI;
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
        }
    }
}
