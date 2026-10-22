using System.Collections;
using JellyRiot.UI;
using JellyRiot.VFX;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace JellyRiot.Tests.PlayMode
{
    public sealed class VFXIntegrationPlayModeTests
    {
        [UnityTest]
        public IEnumerator MainSceneBuildsBoundedProductionVFXPoolAndPreparedLibrary()
        {
            SceneManager.LoadScene("Main");
            yield return null;
            JellyRiotMainUI ui = Object.FindFirstObjectByType<JellyRiotMainUI>();
            Assert.That(ui, Is.Not.Null);
            JellyVFXManager vfx = ui.VFXManager;
            JellyVFXLibrary library = Resources.Load<JellyVFXLibrary>("JellyVFXLibrary");
            Assert.That(vfx, Is.Not.Null);
            Assert.That(vfx.HasCompleteLibrary, Is.True);
            Assert.That(library.Splashes, Has.Length.EqualTo(8));
            Assert.That(library.Droplets, Has.Length.EqualTo(8));
            Assert.That(library.Sparkles, Has.Length.EqualTo(12));
            Assert.That(library.Trails, Has.Length.EqualTo(16));
            Assert.That(library.Glows, Has.Length.EqualTo(12));
            Assert.That(vfx.PoolSize, Is.EqualTo(48));
            Assert.That(vfx.LabelPoolSizeValue, Is.EqualTo(6));
            Assert.That(vfx.AnticipationSeconds, Is.EqualTo(0.12f).Within(0.001f));
            Assert.That(vfx.PopSeconds, Is.EqualTo(0.14f).Within(0.001f));
            Assert.That(vfx.RiotSeconds, Is.InRange(0.5f, 0.8f));
            Assert.That(vfx.RowRiseSeconds, Is.EqualTo(0.24f).Within(0.001f));
            Transform overlay = ui.transform.Find("Jelly Riot Canvas/Safe Area/Gameplay Screen/VFX Overlay");
            Assert.That(overlay, Is.Not.Null);
            Assert.That(overlay.Find("Chain Label").GetComponent<TMPro.TextMeshProUGUI>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator FiftyPlusCellDemonstrationStaysBoundedAndCancellationResetsEverything()
        {
            SceneManager.LoadScene("Main");
            yield return null;
            JellyRiotMainUI ui = Object.FindFirstObjectByType<JellyRiotMainUI>();
            JellyVFXManager vfx = Object.FindFirstObjectByType<JellyVFXManager>();
            Transform overlay = ui.transform.Find("Jelly Riot Canvas/Safe Area/Gameplay Screen/VFX Overlay");
            GameObject riot = overlay.Find("RIOT Celebration").gameObject;
            vfx.Demonstrate(JellyVFXManager.DemonstrationCue.LargeMatch);
            Assert.That(vfx.ActiveParticleCount, Is.LessThanOrEqualTo(vfx.PoolSize));
            Assert.That(vfx.PeakActiveParticleCount, Is.LessThanOrEqualTo(vfx.PoolSize));

            vfx.CancelAndReset();
            Assert.That(vfx.ActiveParticleCount, Is.Zero);
            Assert.That(riot.GetComponent<Image>().gameObject.activeSelf, Is.False);

            vfx.SetReducedMotion(true);
            vfx.Demonstrate(JellyVFXManager.DemonstrationCue.Chain3Riot);
            Assert.That(vfx.ReducedMotion, Is.True);
            Assert.That(vfx.ActiveParticleCount, Is.Zero,
                "Reduced Motion must not emit the large RIOT particle/trail burst.");
            Assert.That(riot.activeSelf, Is.True,
                "Reduced Motion retains readable static RIOT feedback.");
        }
    }
}
