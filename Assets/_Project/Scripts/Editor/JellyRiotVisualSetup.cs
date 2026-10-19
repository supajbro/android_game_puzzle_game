using System;
using System.IO;
using JellyRiot.UI;
using JellyRiot.Core;
using JellyRiot.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JellyRiot.Editor
{
    /// <summary>Repeatable import and scene-authoring workflow for the supplied Work Order 09/10 art.</summary>
    public static class JellyRiotVisualSetup
    {
        private const string Art = "Assets/_Project/Art/";
        private const string Sprites = Art + "Sprites/";
        private const string AnimationFrames = Art + "Animations/Frames/";
        private const string AnimationLibrary = "Assets/_Project/Settings/JellyAnimationLibrary.asset";
        private const string MainScene = "Assets/_Project/Scenes/Main.unity";
        private const string SetupMarker = "Library/JellyRiotVisualSetup.v1";

        [InitializeOnLoadMethod]
        private static void ScheduleInitialBuild()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            EditorApplication.delayCall += BuildIfNeeded;
        }

        private static void BuildIfNeeded()
        {
            if (!File.Exists(SetupMarker))
            {
                BuildMainScene();
            }
            else if (AssetDatabase.LoadAssetAtPath<JellyAnimationLibrary>(AnimationLibrary) == null &&
                     Directory.Exists(AnimationFrames))
            {
                ConfigureAnimationFrames();
            }
        }

        [MenuItem("Jelly Riot/Visuals/Configure Artwork")]
        public static void ConfigureArtwork()
        {
            ConfigureSingle(Art + "background.png", 2048);
            ConfigureSingle(Art + "logo.png", 2048);
            ConfigureSingle(Art + "mascot_icon.png", 2048);
            ConfigureSingle(Art + "overlay.png", 2048);

            string[] jellyFiles = Directory.GetFiles(Sprites, "*.png", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < jellyFiles.Length; i++)
            {
                string path = jellyFiles[i].Replace('\\', '/');
                ConfigureSingle(path, path.EndsWith("GalleryPreview.png", StringComparison.Ordinal) ? 2048 : 512);
            }

            ConfigureAnimationFrames();

#pragma warning disable CS0618
            ConfigureSheet(Art + "button_panel.png", new[]
            {
                Slice("CellHole", 34, 715, 300, 288, 55),
                Slice("RackFive", 345, 710, 645, 292),
                Slice("PillButton", 992, 716, 445, 277, 105),
                Slice("ScorePanel", 12, 188, 665, 324, 120),
                Slice("InformationPanel", 675, 104, 765, 570, 145)
            });
            ConfigureSheet(Art + "buttons.png", new[]
            {
                Slice("Pause", 67, 730, 420, 315),
                Slice("Replay", 530, 727, 395, 319),
                Slice("Sound", 978, 725, 400, 320),
                Slice("Haptics", 66, 376, 422, 330),
                Slice("Home", 520, 360, 407, 347),
                Slice("Daily", 976, 363, 405, 349),
                Slice("Info", 67, 34, 421, 330),
                Slice("SelectionArrow", 548, 56, 374, 302),
                Slice("SelectionHighlight", 995, 3, 387, 370)
            });
#pragma warning restore CS0618
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("Jelly Riot artwork configured: RGBA sprites, bilinear filtering, no mipmaps and named sheet slices.");
        }

        [MenuItem("Jelly Riot/Visuals/Configure Animation Frames")]
        public static void ConfigureAnimationFrames()
        {
            if (!Directory.Exists(AnimationFrames))
            {
                throw new DirectoryNotFoundException(
                    $"Animation frames are missing. Run Tools/Art/prepare_jelly_animation_sheets.py first: {AnimationFrames}");
            }

            string[] frameFiles = Directory.GetFiles(AnimationFrames, "*.png", SearchOption.AllDirectories);
            if (frameFiles.Length != 100)
            {
                throw new InvalidOperationException($"Expected exactly 100 animation frames, found {frameFiles.Length}.");
            }

            string[] colourNames = { "Coral", "Lemon", "Aqua", "Lilac", "Mint" };
            for (int i = 0; i < colourNames.Length; i++)
            {
                ConfigureSingle(Art + $"Animations/Jelly_{colourNames[i]}_Sheet.png", 2048);
            }

            for (int i = 0; i < frameFiles.Length; i++)
            {
                ConfigureSingle(frameFiles[i].Replace('\\', '/'), 256);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            JellyAnimationLibrary library = AssetDatabase.LoadAssetAtPath<JellyAnimationLibrary>(AnimationLibrary);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<JellyAnimationLibrary>();
                AssetDatabase.CreateAsset(library, AnimationLibrary);
            }

            var sets = new JellyAnimationSet[5];
            for (int colourIndex = 0; colourIndex < sets.Length; colourIndex++)
            {
                JellyColour colour = (JellyColour)(colourIndex + 1);
                string colourName = colour.ToString();
                var set = new JellyAnimationSet();
                set.Configure(colour, LoadSingle(Sprites + $"Jelly_{colourName}.png"),
                    LoadAnimationCategory(colourName, "Falling"),
                    LoadAnimationCategory(colourName, "Landing"),
                    LoadAnimationCategory(colourName, "SquashStretch"),
                    LoadAnimationCategory(colourName, "Blink"));
                sets[colourIndex] = set;
            }

            library.Configure(sets);
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log("Jelly Riot animation library configured with 100 ordered RGBA frames.");
        }

        [MenuItem("Jelly Riot/Visuals/Build Main Scene")]
        public static void BuildMainScene()
        {
            ConfigureArtwork();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Jelly Riot UI");
            JellyRiotMainUI ui = root.AddComponent<JellyRiotMainUI>();
            ui.Configure(
                LoadSingle(Art + "background.png"), LoadSingle(Art + "logo.png"),
                LoadSingle(Art + "mascot_icon.png"), LoadSingle(Art + "overlay.png"),
                LoadSlice(Art + "button_panel.png", "CellHole"),
                LoadSlice(Art + "button_panel.png", "PillButton"),
                LoadSlice(Art + "button_panel.png", "ScorePanel"),
                LoadSlice(Art + "button_panel.png", "InformationPanel"),
                LoadSlice(Art + "buttons.png", "Pause"), LoadSlice(Art + "buttons.png", "Replay"),
                LoadSlice(Art + "buttons.png", "Sound"), LoadSlice(Art + "buttons.png", "Haptics"),
                LoadSlice(Art + "buttons.png", "Home"), LoadSlice(Art + "buttons.png", "Daily"),
                LoadSlice(Art + "buttons.png", "Info"), LoadSlice(Art + "buttons.png", "SelectionArrow"),
                LoadSlice(Art + "buttons.png", "SelectionHighlight"),
                new[]
                {
                    LoadSingle(Sprites + "Jelly_Coral.png"), LoadSingle(Sprites + "Jelly_Lemon.png"),
                    LoadSingle(Sprites + "Jelly_Aqua.png"), LoadSingle(Sprites + "Jelly_Lilac.png"),
                    LoadSingle(Sprites + "Jelly_Mint.png")
                }, LoadSingle(Sprites + "Jelly_Shadow.png"),
                AssetDatabase.LoadAssetAtPath<JellyAnimationLibrary>(AnimationLibrary));
            EditorUtility.SetDirty(ui);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MainScene);
            AssetDatabase.SaveAssets();
            File.WriteAllText(SetupMarker, DateTime.UtcNow.ToString("O"));
            Selection.activeGameObject = root;
            Debug.Log("Jelly Riot Main scene rebuilt with supplied visual assets.");
        }

        private static void ConfigureSingle(string path, int maxSize)
        {
            TextureImporter importer = RequireImporter(path);
            ApplyCommon(importer, maxSize);
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.SaveAndReimport();
        }

#pragma warning disable CS0618
        private static void ConfigureSheet(string path, SpriteMetaData[] slices)
        {
            TextureImporter importer = RequireImporter(path);
            ApplyCommon(importer, 2048);
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritesheet = slices;
            importer.SaveAndReimport();
        }

        private static SpriteMetaData Slice(string name, int x, int y, int width, int height, int border = 0)
        {
            return new SpriteMetaData
            {
                name = name,
                rect = new Rect(x, y, width, height),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                border = border <= 0 ? Vector4.zero : new Vector4(border, border, border, border)
            };
        }
#pragma warning restore CS0618

        private static TextureImporter RequireImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException($"Required Jelly Riot artwork is missing or not a texture: {path}");
            }
            return importer;
        }

        private static void ApplyCommon(TextureImporter importer, int maxSize)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = 100f;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.compressionQuality = 70;
            importer.maxTextureSize = maxSize;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android", overridden = true, maxTextureSize = maxSize,
                format = TextureImporterFormat.ASTC_6x6, compressionQuality = 70,
                allowsAlphaSplitting = false
            });
        }

        private static Sprite LoadSingle(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException($"Sprite import failed: {path}");
            return sprite;
        }

        private static Sprite LoadSlice(string path, string name)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite && sprite.name == name) return sprite;
            }
            throw new InvalidOperationException($"Named sprite '{name}' was not imported from {path}.");
        }

        private static Sprite[] LoadAnimationCategory(string colour, string category)
        {
            var frames = new Sprite[5];
            for (int frame = 0; frame < frames.Length; frame++)
            {
                string path = AnimationFrames + $"{colour}/{category}/Jelly_{colour}_{category}_{frame + 1:00}.png";
                frames[frame] = LoadSingle(path);
            }
            return frames;
        }
    }
}
