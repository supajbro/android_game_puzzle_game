using System.IO;
using JellyRiot.Gameplay;
using JellyRiot.Gameplay.Configuration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JellyRiot.Editor
{
    public static class ProjectSetupUtility
    {
        private const string SceneFolder = "Assets/_Project/Scenes";
        private const string BootPath = SceneFolder + "/Boot.unity";
        private const string MainPath = SceneFolder + "/Main.unity";
        private const string BalancePath = "Assets/_Project/Settings/GameBalanceSettings.asset";

        [MenuItem("Jelly Riot/Setup/Refresh Foundation")]
        public static void RefreshFoundation()
        {
            Directory.CreateDirectory(SceneFolder);
            CreateMainScene();
            CreateBootScene();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootPath, true),
                new EditorBuildSettingsScene(MainPath, true)
            };

            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            if (AssetDatabase.LoadAssetAtPath<GameBalanceSettings>(BalancePath) == null)
            {
                GameBalanceSettings settings = ScriptableObject.CreateInstance<GameBalanceSettings>();
                AssetDatabase.CreateAsset(settings, BalancePath);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Jelly Riot foundation refreshed: Boot/Main scenes, build order and portrait settings.");
        }

        private static void CreateBootScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootstrap = new GameObject("Boot Loader");
            bootstrap.AddComponent<BootLoader>();
            EditorSceneManager.SaveScene(scene, BootPath);
        }

        private static void CreateMainScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, MainPath);
        }
    }
}
