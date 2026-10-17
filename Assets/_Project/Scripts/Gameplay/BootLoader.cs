using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JellyRiot.Gameplay
{
    public sealed class BootLoader : MonoBehaviour
    {
        private const string MainSceneName = "Main";

        private IEnumerator Start()
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(MainSceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                Debug.LogError($"Unable to load required scene '{MainSceneName}'.");
                yield break;
            }

            while (!operation.isDone)
            {
                yield return null;
            }
        }
    }
}
