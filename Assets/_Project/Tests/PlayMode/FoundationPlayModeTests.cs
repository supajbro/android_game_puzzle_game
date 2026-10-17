using System.Collections;
using JellyRiot.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JellyRiot.Tests.PlayMode
{
    public sealed class FoundationPlayModeTests
    {
        [UnityTest]
        public IEnumerator BootLoadsMainAndAppliesFrameRateTarget()
        {
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);

            float deadline = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != "Main" && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Main"));
            Assert.That(Application.targetFrameRate, Is.EqualTo(ApplicationStartup.TargetFrameRate));
        }
    }
}
