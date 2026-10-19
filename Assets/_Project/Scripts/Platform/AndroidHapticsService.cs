using UnityEngine;

namespace JellyRiot.Platform
{
    /// <summary>Small Android-only haptics adapter. Unsupported platforms safely do nothing.</summary>
    public sealed class AndroidHapticsService : IHapticsService
    {
        private const string EnabledKey = "JellyRiot.Settings.Haptics";

        public AndroidHapticsService()
        {
            _isEnabled = PlayerPrefs.GetInt(EnabledKey, 1) != 0;
        }

        private bool _isEnabled;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value) return;
                _isEnabled = value;
                PlayerPrefs.SetInt(EnabledKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public bool IsSupported => Application.platform == RuntimePlatform.Android && !Application.isEditor;

        public void Play(HapticCue cue)
        {
            if (!IsEnabled || !IsSupported) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            long duration = cue == HapticCue.Riot ? 45L : cue == HapticCue.Match ? 24L : 14L;
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrator != null && vibrator.Call<bool>("hasVibrator")) vibrator.Call("vibrate", duration);
                }
            }
            catch (AndroidJavaException)
            {
                // Haptics are optional; vendor-specific failures must never interrupt play.
            }
#endif
        }
    }
}
