using System;
using System.Collections.Generic;
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
        private const string AnimationSheets = Art + "Animations/";
        private const string AnimationFrames = AnimationSheets + "Frames40/";
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
            else if (Directory.Exists(AnimationSheets))
            {
                JellyAnimationLibrary library = AssetDatabase.LoadAssetAtPath<JellyAnimationLibrary>(AnimationLibrary);
                if (library == null || !library.IsComplete()) ConfigureAnimationFrames();
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

        [MenuItem("Jelly Riot/Visuals/Configure Animation Frames %#j")]
        public static void ConfigureAnimationFrames()
        {
            string[] colourNames = { "Coral", "Lemon", "Aqua", "Lilac", "Mint" };
            PrepareNormalisedAnimationFrames(colourNames);
            for (int i = 0; i < colourNames.Length; i++)
            {
                ConfigureJellySheet(AnimationSheets + $"Jelly_{colourNames[i]}_Sheet.png", colourNames[i]);
                ConfigureAnimationFrameFiles(colourNames[i]);
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
                set.Configure(colour,
                    LoadNormalisedFrames(colourName, "Idle"),
                    LoadNormalisedFrames(colourName, "Blink"),
                    LoadNormalisedFrames(colourName, "Landing"),
                    LoadNormalisedFrames(colourName, "Recovery"));
                sets[colourIndex] = set;
            }

            library.Configure(sets);
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log("Jelly Riot animation library configured from five cleaned 10 x 4 sheets (200 frames).");
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
                LoadSlice(Art + "buttons.png", "SelectionHighlight"), LoadSingle(Sprites + "Jelly_Shadow.png"),
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
        private static void ConfigureJellySheet(string path, string colourName)
        {
            TextureImporter importer = RequireImporter(path);
            ApplyCommon(importer, 2048);
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            if (width < 10 || height < 4)
                throw new InvalidOperationException($"Expressive jelly sheet is too small: {path} ({width} x {height}).");

            var slices = new SpriteMetaData[40];
            for (int row = 0; row < 4; row++)
            for (int column = 0; column < 10; column++)
            {
                int index = (row * 10) + column;
                int x0 = Mathf.RoundToInt(column * width / 10f);
                int x1 = Mathf.RoundToInt((column + 1) * width / 10f);
                int top = Mathf.RoundToInt(row * height / 4f);
                int bottom = Mathf.RoundToInt((row + 1) * height / 4f);
                slices[index] = new SpriteMetaData
                {
                    name = $"Jelly_{colourName}_Frame_{index:00}",
                    rect = new Rect(x0, height - bottom, x1 - x0, bottom - top),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                };
            }
            importer.spritesheet = slices;
            importer.SaveAndReimport();
        }
#pragma warning restore CS0618

        private sealed class PreparedFrame
        {
            public string Colour;
            public string Animation;
            public int Frame;
            public Color32[] Pixels;
            public int Width;
            public int Height;
            public int Left;
            public int Bottom;
            public int Right;
            public int Top;
        }

        private static readonly string[] AnimationRows = { "Idle", "Blink", "Landing", "Recovery" };

        private static void PrepareNormalisedAnimationFrames(string[] colourNames)
        {
            var prepared = new List<PreparedFrame>(colourNames.Length * 40);
            int maximumWidth = 1;
            int maximumHeight = 1;
            for (int colourIndex = 0; colourIndex < colourNames.Length; colourIndex++)
            {
                string colour = colourNames[colourIndex];
                string path = AnimationSheets + $"Jelly_{colour}_Sheet.png";
                var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!source.LoadImage(File.ReadAllBytes(path), false))
                    throw new InvalidOperationException($"Could not decode animation sheet: {path}");

                Color32[] sourcePixels = source.GetPixels32();
                for (int row = 0; row < 4; row++)
                for (int column = 0; column < 10; column++)
                {
                    int x0 = Mathf.RoundToInt(column * source.width / 10f);
                    int x1 = Mathf.RoundToInt((column + 1) * source.width / 10f);
                    int topFromImage = Mathf.RoundToInt(row * source.height / 4f);
                    int bottomFromImage = Mathf.RoundToInt((row + 1) * source.height / 4f);
                    int y0 = source.height - bottomFromImage;
                    int width = x1 - x0;
                    int height = bottomFromImage - topFromImage;
                    Color32[] crop = CopyCrop(sourcePixels, source.width, x0, y0, width, height);
                    CleanAlpha(crop, width, height, out int left, out int bottom, out int right, out int top);
                    maximumWidth = Mathf.Max(maximumWidth, right - left);
                    maximumHeight = Mathf.Max(maximumHeight, top - bottom);
                    prepared.Add(new PreparedFrame
                    {
                        Colour = colour, Animation = AnimationRows[row], Frame = column,
                        Pixels = crop, Width = width, Height = height,
                        Left = left, Bottom = bottom, Right = right, Top = top
                    });
                }
                UnityEngine.Object.DestroyImmediate(source);
            }

            const int canvasSize = 256;
            const int canvasPadding = 10;
            float scale = Mathf.Min((canvasSize - (canvasPadding * 2f)) / maximumWidth,
                (canvasSize - (canvasPadding * 2f)) / maximumHeight);
            for (int i = 0; i < prepared.Count; i++)
            {
                PreparedFrame frame = prepared[i];
                int outputWidth = Mathf.Max(1, Mathf.RoundToInt((frame.Right - frame.Left) * scale));
                int outputHeight = Mathf.Max(1, Mathf.RoundToInt((frame.Top - frame.Bottom) * scale));
                int outputX = (canvasSize - outputWidth) / 2;
                int outputY = canvasPadding;
                Color32[] output = Resample(frame, canvasSize, outputX, outputY, outputWidth, outputHeight);
                var texture = new Texture2D(canvasSize, canvasSize, TextureFormat.RGBA32, false);
                texture.SetPixels32(output);
                texture.Apply(false, false);
                string directory = AnimationFrames + frame.Colour + "/" + frame.Animation;
                Directory.CreateDirectory(directory);
                string outputPath = directory + $"/Jelly_{frame.Colour}_{frame.Animation}_{frame.Frame:00}.png";
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static Color32[] CopyCrop(Color32[] source, int sourceWidth, int x, int y, int width, int height)
        {
            var result = new Color32[width * height];
            for (int row = 0; row < height; row++)
                Array.Copy(source, ((y + row) * sourceWidth) + x, result, row * width, width);
            return result;
        }

        private static void CleanAlpha(Color32[] pixels, int width, int height,
            out int left, out int bottom, out int right, out int top)
        {
            var opaque = new bool[pixels.Length];
            for (int i = 0; i < pixels.Length; i++) opaque[i] = pixels[i].a >= 12;
            bool[] opened = Dilate(Erode(Erode(opaque, width, height), width, height), width, height);
            opened = Dilate(opened, width, height);
            bool[] body = LargestCentralComponent(opened, width, height);
            for (int i = 0; i < 4; i++) body = Dilate(body, width, height);

            left = width;
            bottom = height;
            right = 0;
            top = 0;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                if (!body[index]) pixels[index] = new Color32(0, 0, 0, 0);
                if (pixels[index].a < 12) continue;
                left = Mathf.Min(left, x);
                bottom = Mathf.Min(bottom, y);
                right = Mathf.Max(right, x + 1);
                top = Mathf.Max(top, y + 1);
            }
            if (right <= left || top <= bottom)
                throw new InvalidOperationException("Jelly cleanup removed an entire animation frame.");
        }

        private static bool[] Erode(bool[] source, int width, int height)
        {
            var result = new bool[source.Length];
            for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
            {
                bool keep = true;
                for (int offsetY = -1; offsetY <= 1 && keep; offsetY++)
                for (int offsetX = -1; offsetX <= 1; offsetX++)
                    if (!source[((y + offsetY) * width) + x + offsetX]) { keep = false; break; }
                result[(y * width) + x] = keep;
            }
            return result;
        }

        private static bool[] Dilate(bool[] source, int width, int height)
        {
            var result = new bool[source.Length];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                bool keep = false;
                for (int offsetY = -1; offsetY <= 1 && !keep; offsetY++)
                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    int sampleX = x + offsetX;
                    int sampleY = y + offsetY;
                    if (sampleX >= 0 && sampleX < width && sampleY >= 0 && sampleY < height &&
                        source[(sampleY * width) + sampleX]) { keep = true; break; }
                }
                result[(y * width) + x] = keep;
            }
            return result;
        }

        private static bool[] LargestCentralComponent(bool[] source, int width, int height)
        {
            var visited = new bool[source.Length];
            List<int> best = null;
            var queue = new Queue<int>();
            for (int start = 0; start < source.Length; start++)
            {
                if (!source[start] || visited[start]) continue;
                var component = new List<int>();
                bool central = false;
                visited[start] = true;
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    int index = queue.Dequeue();
                    component.Add(index);
                    int x = index % width;
                    int y = index / width;
                    central |= Mathf.Abs(x - (width * 0.5f)) < width * 0.22f &&
                        Mathf.Abs(y - (height * 0.5f)) < height * 0.25f;
                    EnqueueNeighbour(x - 1, y, width, height, source, visited, queue);
                    EnqueueNeighbour(x + 1, y, width, height, source, visited, queue);
                    EnqueueNeighbour(x, y - 1, width, height, source, visited, queue);
                    EnqueueNeighbour(x, y + 1, width, height, source, visited, queue);
                }
                if (central && (best == null || component.Count > best.Count)) best = component;
            }
            if (best == null) throw new InvalidOperationException("No central jelly body was found in an animation frame.");
            var result = new bool[source.Length];
            for (int i = 0; i < best.Count; i++) result[best[i]] = true;
            return result;
        }

        private static void EnqueueNeighbour(int x, int y, int width, int height, bool[] source,
            bool[] visited, Queue<int> queue)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return;
            int index = (y * width) + x;
            if (!source[index] || visited[index]) return;
            visited[index] = true;
            queue.Enqueue(index);
        }

        private static Color32[] Resample(PreparedFrame frame, int canvasSize, int outputX, int outputY,
            int outputWidth, int outputHeight)
        {
            var result = new Color32[canvasSize * canvasSize];
            float sourceWidth = frame.Right - frame.Left;
            float sourceHeight = frame.Top - frame.Bottom;
            for (int y = 0; y < outputHeight; y++)
            for (int x = 0; x < outputWidth; x++)
            {
                float sourceX = frame.Left + (((x + 0.5f) / outputWidth) * sourceWidth) - 0.5f;
                float sourceY = frame.Bottom + (((y + 0.5f) / outputHeight) * sourceHeight) - 0.5f;
                result[((outputY + y) * canvasSize) + outputX + x] =
                    Bilinear(frame.Pixels, frame.Width, frame.Height, sourceX, sourceY);
            }
            return result;
        }

        private static Color32 Bilinear(Color32[] pixels, int width, int height, float x, float y)
        {
            int x0 = Mathf.Clamp(Mathf.FloorToInt(x), 0, width - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(y), 0, height - 1);
            int x1 = Mathf.Min(x0 + 1, width - 1);
            int y1 = Mathf.Min(y0 + 1, height - 1);
            float tx = Mathf.Clamp01(x - x0);
            float ty = Mathf.Clamp01(y - y0);
            Color bottom = Color.Lerp((Color)pixels[(y0 * width) + x0], (Color)pixels[(y0 * width) + x1], tx);
            Color top = Color.Lerp((Color)pixels[(y1 * width) + x0], (Color)pixels[(y1 * width) + x1], tx);
            return (Color32)Color.Lerp(bottom, top, ty);
        }

        private static void ConfigureAnimationFrameFiles(string colour)
        {
            for (int row = 0; row < AnimationRows.Length; row++)
            for (int frame = 0; frame < 10; frame++)
            {
                string path = AnimationFrames + colour + "/" + AnimationRows[row] +
                    $"/Jelly_{colour}_{AnimationRows[row]}_{frame:00}.png";
                TextureImporter importer = RequireImporter(path);
                ApplyCommon(importer, 256);
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePivot = new Vector2(0.5f, 0.5f);
                importer.SaveAndReimport();
            }
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

        private static Sprite[] LoadNormalisedFrames(string colour, string animation)
        {
            var frames = new Sprite[10];
            for (int frame = 0; frame < frames.Length; frame++)
            {
                string path = AnimationFrames + colour + "/" + animation +
                    $"/Jelly_{colour}_{animation}_{frame:00}.png";
                frames[frame] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (frames[frame] == null)
                    throw new InvalidOperationException($"Normalised expressive frame was not imported: {path}");
            }
            return frames;
        }
    }
}
