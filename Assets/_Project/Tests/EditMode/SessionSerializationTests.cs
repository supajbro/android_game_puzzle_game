using JellyRiot.Core;
using NUnit.Framework;
using UnityEngine;

namespace JellyRiot.Tests.EditMode
{
    public sealed class SessionSerializationTests
    {
        [Test]
        public void SessionSnapshotRoundTripsThroughUnityJson()
        {
            var original = new GameSession();
            original.CompleteBoot();
            original.StartNewRun(424242UL, GameMode.Daily, "2026-10-17");
            original.SelectRackSlot(2);
            original.TryBeginTurn(4);
            original.CompleteTurn();

            string json = JsonUtility.ToJson(original.CreateSnapshot());
            SessionSnapshot snapshot = JsonUtility.FromJson<SessionSnapshot>(json);
            var restored = new GameSession();
            restored.Restore(snapshot);

            Assert.That(restored.Seed, Is.EqualTo(original.Seed));
            Assert.That(restored.Mode, Is.EqualTo(GameMode.Daily));
            Assert.That(restored.DailyKey, Is.EqualTo("2026-10-17"));
            Assert.That(restored.State, Is.EqualTo(original.State));
            Assert.That(restored.Board.CopyCells(), Is.EqualTo(original.Board.CopyCells()));
            Assert.That(restored.Rack, Is.EqualTo(original.Rack));
            Assert.That(restored.Score.CurrentScore, Is.EqualTo(original.Score.CurrentScore));
            Assert.That(restored.NextRowIn, Is.EqualTo(original.NextRowIn));
        }
    }
}
