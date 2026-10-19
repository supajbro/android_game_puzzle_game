using System;
using System.IO;
using JellyRiot.Audio;
using UnityEditor;
using UnityEngine;

namespace JellyRiot.Editor
{
    public static class JellyRiotAudioSetup
    {
        private const string Root = "Assets/_Project/Audio/SFX/";
        private const string ResourceDirectory = "Assets/_Project/Resources";
        private const string SettingsPath = ResourceDirectory + "/JellyRiotAudioSettings.asset";

        [InitializeOnLoadMethod]
        private static void ScheduleConfiguration()
        {
            EditorApplication.delayCall += () =>
            {
                if (!Application.isBatchMode && AssetDatabase.LoadAssetAtPath<JellyRiotAudioSettings>(SettingsPath) == null)
                    ConfigureAudio();
            };
        }

        [MenuItem("Jelly Riot/Audio/Configure SFX")]
        public static void ConfigureAudio()
        {
            string[] paths = Directory.GetFiles(Root, "*.wav", SearchOption.AllDirectories);
            if (paths.Length != 26) throw new InvalidOperationException($"Expected 26 WAV files, found {paths.Length}.");
            for (int i = 0; i < paths.Length; i++) ConfigureImporter(paths[i].Replace('\\', '/'));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            if (!AssetDatabase.IsValidFolder(ResourceDirectory))
                AssetDatabase.CreateFolder("Assets/_Project", "Resources");
            JellyRiotAudioSettings settings = AssetDatabase.LoadAssetAtPath<JellyRiotAudioSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<JellyRiotAudioSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            settings.Configure(
                Clips("UI/UI_Tap_", 1, 3), Clip("UI/UI_Invalid"), Clip("UI/Score_Tick"),
                Clips("Jelly/Jelly_Drop_", 1, 4), Clips("Jelly/Jelly_Land_", 1, 3),
                Clips("Jelly/Jelly_Pop_", 1, 5), Clips("Cascades/Cascade_", 2, 5),
                Clip("Cascades/Riot_Stinger"), Clip("Game/Row_Rise"), Clip("Game/Game_Over"),
                Clip("Game/Best_Score"), Clip("Game/Daily_Complete"));
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("Jelly Riot audio configured: 26 pooled 2D SFX references and Android import settings.");
        }

        [MenuItem("Jelly Riot/Audio/Open SFX Preview")]
        private static void OpenPreview() => EditorWindow.GetWindow<JellyRiotAudioPreviewWindow>("Jelly Riot SFX");

        private static void ConfigureImporter(string path)
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) throw new FileNotFoundException("Audio clip importer missing.", path);
            importer.forceToMono = true;
            importer.loadInBackground = false;
            importer.ambisonic = false;
            var sample = new AudioImporterSampleSettings
            {
                loadType = AudioClipLoadType.DecompressOnLoad,
                compressionFormat = AudioCompressionFormat.Vorbis,
                quality = 0.76f,
                sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate,
                preloadAudioData = true
            };
            importer.defaultSampleSettings = sample;
            importer.SetOverrideSampleSettings("Android", sample);
            importer.SaveAndReimport();
        }

        private static AudioClip Clip(string relative)
        {
            string path = Root + relative + ".wav";
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) throw new FileNotFoundException("Required audio clip missing.", path);
            return clip;
        }

        private static AudioClip[] Clips(string prefix, int first, int last)
        {
            var clips = new AudioClip[(last - first) + 1];
            for (int value = first; value <= last; value++) clips[value - first] = Clip(prefix + value.ToString("00"));
            return clips;
        }
    }

    public sealed class JellyRiotAudioPreviewWindow : EditorWindow
    {
        private Vector2 _scroll;

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Enter Play Mode and open Main to preview through the production voice pool.", MessageType.Info);
            JellyRiotAudioSettings settings = AssetDatabase.LoadAssetAtPath<JellyRiotAudioSettings>(
                "Assets/_Project/Resources/JellyRiotAudioSettings.asset");
            AudioManager manager = FindFirstObjectByType<AudioManager>();
            using (new EditorGUI.DisabledScope(!Application.isPlaying || manager == null || settings == null))
            {
                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                if (settings != null)
                {
                    DrawGroup(manager, "UI taps", settings.UiTaps);
                    Draw(manager, settings.UiInvalid); Draw(manager, settings.ScoreTick);
                    DrawGroup(manager, "Drops", settings.Drops);
                    DrawGroup(manager, "Landings", settings.Landings);
                    DrawGroup(manager, "Pops", settings.Pops);
                    DrawGroup(manager, "Cascades", settings.Cascades);
                    Draw(manager, settings.RiotStinger); Draw(manager, settings.RowRise);
                    Draw(manager, settings.GameOver); Draw(manager, settings.BestScore);
                    Draw(manager, settings.DailyComplete);
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private static void DrawGroup(AudioManager manager, string label, AudioClip[] clips)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            if (clips == null) return;
            for (int i = 0; i < clips.Length; i++) Draw(manager, clips[i]);
        }

        private static void Draw(AudioManager manager, AudioClip clip)
        {
            if (clip != null && GUILayout.Button(clip.name)) manager.PlayPreview(clip);
        }
    }
}
