using UnityEngine;

namespace JellyRiot.Gameplay
{
    public static class ApplicationStartup
    {
        public const int TargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ConfigureApplication()
        {
            Application.targetFrameRate = TargetFrameRate;
        }
    }
}
