using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JellyRiot.VFX;
using TMPro;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace JellyRiot.Editor
{
    /// <summary>Prepares the supplied VFX sheets with deliberate, artefact-safe rectangles.</summary>
    [InitializeOnLoad]
    public static class JellyRiotVFXSetup
    {
        private const string ArtRoot = "Assets/_Project/Art/vfx/";
        private const string LibraryPath = "Assets/_Project/Resources/JellyVFXLibrary.asset";

        static JellyRiotVFXSetup()
        {
            EditorApplication.delayCall += ConfigureIfMissing;
        }

        private static void ConfigureIfMissing()
        {
            if (Application.isBatchMode) return;
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                TMP_PackageResourceImporter.ImportResources(true, false, false);
            }
            bool prepared = AssetDatabase.LoadAllAssetsAtPath(ArtRoot + "VFX_JellyPop.png")
                .OfType<Sprite>().Any(sprite => sprite.name == "JellyPop_01");
            if (prepared && AssetDatabase.LoadAssetAtPath<JellyVFXLibrary>(LibraryPath) != null) return;
            ConfigureVFX();
        }

        [MenuItem("Jelly Riot/VFX/Prepare Artwork and Library")]
        public static void ConfigureVFX()
        {
            ConfigureGrid("VFX_JellyPop.png", 4, 4, "JellyPop");
            ConfigureGrid("VFX_Sparkles.png", 3, 4, "Sparkle");
            ConfigureGrid("VFX_Trails.png", 4, 4, "Trail");
            ConfigureGrid("VFX_Glows.png", 3, 4, "Glow");
            ConfigureCustom("VFX_Warnings.png", new[]
            {
                Rect("Warning_OneMove", 0, 627, 627, 627),
                Rect("Warning_RowRise", 627, 627, 627, 627),
                Rect("Warning_AlertBubble", 0, 0, 313, 627),
                Rect("Warning_Countdown", 313, 0, 314, 627),
                Rect("Warning_Arrow", 627, 0, 313, 627),
                Rect("Warning_Alert", 940, 0, 314, 627)
            });
            ConfigureSingle("VFX_Riot.png");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Sprite[] pops = LoadSprites("VFX_JellyPop.png");
            Sprite[] sparkles = LoadSprites("VFX_Sparkles.png");
            Sprite[] trails = LoadSprites("VFX_Trails.png");
            Sprite[] glows = LoadSprites("VFX_Glows.png");
            Sprite[] warnings = LoadSprites("VFX_Warnings.png");
            Sprite riot = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "VFX_Riot.png");

            JellyVFXLibrary library = AssetDatabase.LoadAssetAtPath<JellyVFXLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<JellyVFXLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            library.Configure(pops.Take(8).ToArray(), pops.Skip(8).ToArray(), sparkles, trails, glows,
                riot, Find(warnings, "Warning_OneMove"), Find(warnings, "Warning_RowRise"));
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log($"Jelly Riot VFX prepared: {pops.Length} pops, {sparkles.Length} sparkles, " +
                      $"{trails.Length} trails, {glows.Length} glows and {warnings.Length} warnings.");
        }

        private static void ConfigureGrid(string fileName, int columns, int rows, string prefix)
        {
            string path = ArtRoot + fileName;
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) throw new FileNotFoundException("VFX texture not found.", path);
            float cellWidth = texture.width / (float)columns;
            float cellHeight = texture.height / (float)rows;
            var rects = new List<SpriteRect>(columns * rows);
            int index = 1;
            for (int row = rows - 1; row >= 0; row--)
            {
                for (int column = 0; column < columns; column++)
                {
                    int left = Mathf.RoundToInt(column * cellWidth);
                    int right = Mathf.RoundToInt((column + 1) * cellWidth);
                    int bottom = Mathf.RoundToInt(row * cellHeight);
                    int top = Mathf.RoundToInt((row + 1) * cellHeight);
                    rects.Add(Rect($"{prefix}_{index:00}", left, bottom, right - left, top - bottom));
                    index++;
                }
            }
            ConfigureCustom(fileName, rects.ToArray());
        }

        private static void ConfigureCustom(string fileName, SpriteRect[] rects)
        {
            string path = ArtRoot + fileName;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("VFX texture importer not found.", path);
            ConfigureImporter(importer, true, 2048);

            var factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var existing = provider.GetSpriteRects().ToDictionary(value => value.name, value => value.spriteID);
            for (int i = 0; i < rects.Length; i++)
                rects[i].spriteID = existing.TryGetValue(rects[i].name, out GUID id) ? id : GUID.Generate();
            provider.SetSpriteRects(rects);
            provider.Apply();
            importer.SaveAndReimport();
        }

        private static void ConfigureSingle(string fileName)
        {
            string path = ArtRoot + fileName;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("VFX texture importer not found.", path);
            ConfigureImporter(importer, false, 4096);
            importer.SaveAndReimport();
        }

        private static void ConfigureImporter(TextureImporter importer, bool multiple, int maximumSize)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = multiple ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = maximumSize;
            importer.spritePixelsPerUnit = 100f;
        }

        private static SpriteRect Rect(string name, int x, int y, int width, int height) => new SpriteRect
        {
            name = name,
            rect = new Rect(x, y, width, height),
            alignment = SpriteAlignment.Center,
            pivot = new Vector2(0.5f, 0.5f)
        };

        private static Sprite[] LoadSprites(string fileName) => AssetDatabase.LoadAllAssetsAtPath(ArtRoot + fileName)
            .OfType<Sprite>().OrderBy(sprite => sprite.name, StringComparer.Ordinal).ToArray();

        private static Sprite Find(Sprite[] sprites, string name)
        {
            Sprite sprite = sprites.FirstOrDefault(value => value.name == name);
            if (sprite == null) throw new InvalidOperationException($"Required VFX sprite '{name}' was not imported.");
            return sprite;
        }
    }

    public sealed class JellyRiotVFXPreviewWindow : EditorWindow
    {
        [MenuItem("Jelly Riot/VFX/Open Demonstration")]
        private static void Open() => GetWindow<JellyRiotVFXPreviewWindow>("Jelly Riot VFX");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Production VFX demonstration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Enter Play Mode and open the Gameplay screen. These buttons call the live pooled JellyVFXManager.", MessageType.Info);
            JellyVFXManager manager = FindFirstObjectByType<JellyVFXManager>();
            using (new EditorGUI.DisabledScope(!Application.isPlaying || manager == null))
            {
                Draw(manager, "Normal jelly pop", JellyVFXManager.DemonstrationCue.NormalPop);
                Draw(manager, "Large match pop", JellyVFXManager.DemonstrationCue.LargeMatch);
                Draw(manager, "CHAIN x2", JellyVFXManager.DemonstrationCue.Chain2);
                Draw(manager, "CHAIN x3 / RIOT!", JellyVFXManager.DemonstrationCue.Chain3Riot);
                Draw(manager, "Score fly-up", JellyVFXManager.DemonstrationCue.ScoreFlyUp);
                Draw(manager, "One-move warning", JellyVFXManager.DemonstrationCue.OneMoveWarning);
                Draw(manager, "Pressure-row rise", JellyVFXManager.DemonstrationCue.PressureRowRise);
            }
        }

        private static void Draw(JellyVFXManager manager, string label, JellyVFXManager.DemonstrationCue cue)
        {
            if (GUILayout.Button(label, GUILayout.Height(30f))) manager.Demonstrate(cue);
        }
    }

    public static class JellyRiotVFXTestRunner
    {
        [MenuItem("Jelly Riot/VFX/Run PlayMode Tests _F8")]
        public static void RunPlayModeTests()
        {
            var runner = ScriptableObject.CreateInstance<TestRunnerApi>();
            runner.RegisterCallbacks(new CompletionCallbacks());
            runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode }));
        }

        private sealed class CompletionCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) =>
                Debug.Log("[WO14 TESTS] PlayMode run started.");

            public void RunFinished(ITestResultAdaptor result) =>
                Debug.Log($"[WO14 TESTS] Finished: {result.PassCount} passed, {result.FailCount} failed, " +
                          $"{result.SkipCount} skipped, {result.InconclusiveCount} inconclusive.");

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.TestStatus == TestStatus.Failed)
                    Debug.LogError($"[WO14 TESTS] {result.FullName}: {result.Message}\n{result.StackTrace}");
            }
        }
    }
}
