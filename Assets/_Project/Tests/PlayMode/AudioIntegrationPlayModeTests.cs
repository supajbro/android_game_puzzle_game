using System.Collections;
using JellyRiot.Audio;
using JellyRiot.Core;
using JellyRiot.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JellyRiot.Tests.PlayMode
{
    public sealed class AudioIntegrationPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            PlayerPrefs.DeleteKey("JellyRiot.Settings.AudioMuted");
            PlayerPrefs.DeleteKey("JellyRiot.Settings.MasterVolume");
            PlayerPrefs.DeleteKey("JellyRiot.Settings.SfxVolume");
            PlayerPrefs.DeleteKey("JellyRiot.Settings.MusicVolume");
            PlayerPrefs.DeleteKey("JellyRiot.Settings.Haptics");
            SceneManager.LoadScene("Main");
            yield return null;
        }

        [UnityTest]
        public IEnumerator AllRequiredClipsUseBoundedTwoDimensionalPool()
        {
            AudioManager manager = Object.FindFirstObjectByType<AudioManager>();
            Assert.That(manager, Is.Not.Null);
            Assert.That(manager.Settings, Is.Not.Null);
            Assert.That(manager.Settings.HasAllRequiredClips, Is.True);
            Assert.That(manager.VoiceCount, Is.EqualTo(manager.Settings.VoiceLimit));
            Assert.That(manager.VoiceCount, Is.InRange(4, 16));

            AudioSource[] sources = manager.GetComponentsInChildren<AudioSource>();
            Assert.That(sources.Length, Is.EqualTo(manager.VoiceCount + 1));
            for (int i = 0; i < sources.Length; i++)
                Assert.That(sources[i].spatialBlend, Is.Zero, $"{sources[i].name} must be 2D.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator DuplicateCoreActionIsConsumedExactlyOnceAndPoolDoesNotGrow()
        {
            JellyRiotMainUI ui = Object.FindFirstObjectByType<JellyRiotMainUI>();
            AudioManager manager = ui.AudioManager;
            GameObject.Find("Play Button").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            SessionActionResult selected = ui.Session.SelectRackSlot(0);
            int sourceCount = manager.GetComponentsInChildren<AudioSource>().Length;

            manager.ProcessAction(selected);
            int issued = manager.IssuedSoundCount;
            manager.ProcessAction(selected);

            Assert.That(manager.IssuedSoundCount, Is.EqualTo(issued));
            Assert.That(manager.LastProcessedEventSequence, Is.EqualTo(selected.Events[selected.Events.Count - 1].Sequence));
            Assert.That(manager.GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(sourceCount));
            yield return null;
        }

        [UnityTest]
        public IEnumerator MuteVolumesAndHapticsPersistWithoutAffectingGameplay()
        {
            JellyRiotMainUI ui = Object.FindFirstObjectByType<JellyRiotMainUI>();
            AudioManager manager = ui.AudioManager;
            manager.MasterVolume = 0.72f;
            manager.SfxVolume = 0.63f;
            manager.MusicVolume = 0.21f;
            manager.IsMuted = true;
            manager.Haptics.IsEnabled = false;

            ui.Session.StartNewRun(42UL);
            Assert.That(ui.Session.SelectRackSlot(0).Succeeded, Is.True);
            Assert.That(ui.Session.TryBeginTurn(0).Succeeded, Is.True,
                "Core gameplay must continue while all feedback is disabled.");

            Object.Destroy(manager);
            yield return null;
            AudioManager restored = ui.gameObject.AddComponent<AudioManager>();
            yield return null;
            Assert.That(restored.IsMuted, Is.True);
            Assert.That(restored.MasterVolume, Is.EqualTo(0.72f).Within(0.001f));
            Assert.That(restored.SfxVolume, Is.EqualTo(0.63f).Within(0.001f));
            Assert.That(restored.MusicVolume, Is.EqualTo(0.21f).Within(0.001f));
            Assert.That(restored.Haptics.IsEnabled, Is.False);
        }

        [UnityTest]
        public IEnumerator PauseFocusAndAdvertisementInterruptionsClearDelayedSounds()
        {
            AudioManager manager = Object.FindFirstObjectByType<AudioManager>();
            manager.PlayUiTap();
            manager.SetPaused(true);
            Assert.That(manager.PendingSoundCount, Is.Zero);
            manager.SetPaused(false);
            manager.SetExternalInterruption(true);
            manager.PlayUiTap();
            Assert.That(manager.PendingSoundCount, Is.Zero);
            manager.SetExternalInterruption(false);
            manager.PlayUiTap();
            Assert.That(manager.IssuedSoundCount, Is.GreaterThan(0));
            yield return null;
        }
    }
}
