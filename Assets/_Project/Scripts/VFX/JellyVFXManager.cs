using System;
using System.Collections.Generic;
using JellyRiot.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JellyRiot.VFX
{
    /// <summary>
    /// Bounded, pooled presentation for authoritative Core resolution results. It never computes or
    /// mutates gameplay outcomes; callbacks let the board view advance at semantic transition points.
    /// </summary>
    public sealed class JellyVFXManager : MonoBehaviour
    {
        public enum DemonstrationCue
        {
            NormalPop,
            LargeMatch,
            Chain2,
            Chain3Riot,
            ScoreFlyUp,
            OneMoveWarning,
            PressureRowRise
        }

        private enum PresentationPhase { None, Anticipation, Pop, RowRise }

        private sealed class PooledSprite
        {
            public Image Image;
            public RectTransform Rect;
            public Vector2 Start;
            public Vector2 Velocity;
            public float StartRotation;
            public float RotationSpeed;
            public float StartScale;
            public float EndScale;
            public float Age;
            public float Lifetime;
            public Color Colour;
            public bool Active;
        }

        private sealed class PooledLabel
        {
            public TextMeshProUGUI Text;
            public RectTransform Rect;
            public Vector2 Start;
            public float Age;
            public float Lifetime;
            public bool Active;
        }

        private const string ResourceName = "JellyVFXLibrary";
        private const int ParticlePoolSize = 48;
        private const int LabelPoolSize = 6;
        private const int MaximumParticlesPerGroup = 10;
        private const float AnticipationDuration = 0.12f;
        private const float PopDuration = 0.14f;
        private const float RiotDuration = 0.68f;
        private const float RowRiseDuration = 0.24f;

        private readonly PooledSprite[] _particles = new PooledSprite[ParticlePoolSize];
        private readonly PooledLabel[] _labels = new PooledLabel[LabelPoolSize];
        private readonly List<RectTransform> _anticipatedRoots = new List<RectTransform>(BoardModel.CellCount);
        private readonly System.Random _cosmeticRandom = new System.Random();
        private JellyVFXLibrary _library;
        private RectTransform _overlay;
        private RectTransform _boardRoot;
        private Image[] _boardImages;
        private RectTransform _scoreTarget;
        private Image _glow;
        private Image _riot;
        private Image _warning;
        private TextMeshProUGUI _chainText;
        private TMP_FontAsset _font;
        private SessionActionResult _action;
        private BoardResolutionResult _resolution;
        private ScoreResolutionResult _scoring;
        private Action<ResolutionWave> _applyWave;
        private Action _applyPressureRow;
        private Action _finished;
        private PresentationPhase _phase;
        private int _waveCursor;
        private float _phaseTime;
        private float _riotTime;
        private float _chainTime;
        private float _glowTime;
        private float _warningTime;
        private Vector2 _boardRestPosition;
        private bool _showingPressureWaves;
        private bool _pressureApplied;
        private bool _riotShownForAction;
        private bool _reducedMotion;
        private bool _paused;
        private bool _oneMoveVisible;

        public bool IsPresenting => _phase != PresentationPhase.None;
        public bool ReducedMotion => _reducedMotion;
        public int PoolSize => ParticlePoolSize;
        public int LabelPoolSizeValue => LabelPoolSize;
        public int ActiveParticleCount { get; private set; }
        public int PeakActiveParticleCount { get; private set; }
        public int RiotRevealCount { get; private set; }
        public bool HasCompleteLibrary => _library != null && _library.IsComplete;
        public float AnticipationSeconds => AnticipationDuration;
        public float PopSeconds => PopDuration;
        public float RiotSeconds => RiotDuration;
        public float RowRiseSeconds => RowRiseDuration;

        public void Initialise(JellyVFXLibrary library, RectTransform overlay, RectTransform boardRoot,
            Image[] boardImages, RectTransform scoreTarget)
        {
            _library = library != null ? library : Resources.Load<JellyVFXLibrary>(ResourceName);
            if (_library == null || !_library.IsComplete)
                throw new ArgumentException("A complete Jelly VFX library is required.", nameof(library));
            if (overlay == null) throw new ArgumentNullException(nameof(overlay));
            if (boardRoot == null) throw new ArgumentNullException(nameof(boardRoot));
            if (boardImages == null || boardImages.Length != BoardModel.CellCount)
                throw new ArgumentException($"Expected {BoardModel.CellCount} board images.", nameof(boardImages));

            _overlay = overlay;
            _boardRoot = boardRoot;
            _boardImages = boardImages;
            _scoreTarget = scoreTarget;
            _boardRestPosition = boardRoot.anchoredPosition;
            _font = ResolveFont();
            BuildPersistentViews();
            BuildPools();
            CancelAndReset();
        }

        public void SetReducedMotion(bool reducedMotion)
        {
            _reducedMotion = reducedMotion;
            if (reducedMotion)
            {
                _boardRoot.anchoredPosition = _boardRestPosition;
                ResetAnticipation();
            }
        }

        public void SetPaused(bool paused) => _paused = paused;

        public void ShowOneMoveWarning(bool show)
        {
            if (!show)
            {
                _oneMoveVisible = false;
                if (_phase != PresentationPhase.RowRise) HideWarning();
                return;
            }
            if (_oneMoveVisible) return;
            _oneMoveVisible = true;
            ShowWarning(_library.OneMoveWarning, _reducedMotion ? 1.2f : 1.6f);
        }

        public void PresentAction(SessionActionResult action, Action<ResolutionWave> applyWave,
            Action applyPressureRow, Action finished)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            CancelAndReset();
            _action = action;
            _applyWave = applyWave;
            _applyPressureRow = applyPressureRow;
            _finished = finished;
            _resolution = action.Resolution;
            _scoring = action.Scoring;
            _waveCursor = 0;
            _pressureApplied = action.PressureRow == null;
            _showingPressureWaves = false;
            _riotShownForAction = false;
            _riotTime = 0f;
            _chainTime = 0f;
            _glowTime = 0f;
            _warningTime = 0f;
            _oneMoveVisible = false;
            if (HasWave(_resolution, 0)) BeginWave();
            else if (!_pressureApplied) BeginRowRise();
            else CompletePresentation();
        }

        public void CancelAndReset()
        {
            ResetAnticipation();
            if (_boardRoot != null) _boardRoot.anchoredPosition = _boardRestPosition;
            _phase = PresentationPhase.None;
            _action = null;
            _resolution = null;
            _scoring = null;
            _applyWave = null;
            _applyPressureRow = null;
            _finished = null;
            _riotShownForAction = false;
            for (int i = 0; i < _particles.Length; i++) Deactivate(_particles[i]);
            for (int i = 0; i < _labels.Length; i++) Deactivate(_labels[i]);
            if (_glow != null) _glow.gameObject.SetActive(false);
            if (_riot != null) _riot.gameObject.SetActive(false);
            if (_chainText != null) _chainText.gameObject.SetActive(false);
            HideWarning();
            ActiveParticleCount = 0;
        }

        public void Demonstrate(DemonstrationCue cue)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Vector2 centre = BoardLocalToOverlay(new BoardCoordinate(3, 4));
            switch (cue)
            {
                case DemonstrationCue.NormalPop:
                    SpawnBurst(centre, new Color32(255, 108, 135, 255), 8, false);
                    break;
                case DemonstrationCue.LargeMatch:
                    for (int i = 0; i < 8; i++)
                        SpawnBurst(centre + RandomInside(150f), ColourFor((JellyColour)((i % 5) + 1)), 8, true);
                    break;
                case DemonstrationCue.Chain2:
                    ShowChain(2);
                    ShowGlow(0.42f);
                    break;
                case DemonstrationCue.Chain3Riot:
                    ShowChain(3);
                    ShowRiot();
                    break;
                case DemonstrationCue.ScoreFlyUp:
                    SpawnScoreLabel(centre, 480L);
                    break;
                case DemonstrationCue.OneMoveWarning:
                    _oneMoveVisible = false;
                    ShowOneMoveWarning(true);
                    break;
                case DemonstrationCue.PressureRowRise:
                    ShowWarning(_library.RowRiseWarning, 0.7f);
                    _boardRoot.anchoredPosition = _boardRestPosition + Vector2.down * 82f;
                    _phase = PresentationPhase.RowRise;
                    _phaseTime = 0f;
                    _pressureApplied = true;
                    break;
            }
#endif
        }

        private void Update()
        {
            if (_paused || _overlay == null) return;
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) return;
            TickParticles(deltaTime);
            TickLabels(deltaTime);
            TickPersistentEffects(deltaTime);
            TickPresentation(deltaTime);
        }

        private void TickPresentation(float deltaTime)
        {
            if (_phase == PresentationPhase.None) return;
            _phaseTime += deltaTime;
            switch (_phase)
            {
                case PresentationPhase.Anticipation:
                    TickAnticipation();
                    if (_phaseTime >= (_reducedMotion ? 0.04f : AnticipationDuration)) ReleaseWave();
                    break;
                case PresentationPhase.Pop:
                    if (_phaseTime >= (_reducedMotion ? 0.05f : PopDuration)) AdvanceAfterWave();
                    break;
                case PresentationPhase.RowRise:
                    float duration = _reducedMotion ? 0.08f : RowRiseDuration;
                    float t = Mathf.Clamp01(_phaseTime / duration);
                    _boardRoot.anchoredPosition = Vector2.Lerp(_boardRestPosition + Vector2.down * 82f,
                        _boardRestPosition, EaseOutCubic(t));
                    if (_phaseTime >= duration) AdvanceAfterRowRise();
                    break;
            }
        }

        private void BeginWave()
        {
            _phase = PresentationPhase.Anticipation;
            _phaseTime = 0f;
            ResolutionWave wave = _resolution.Waves[_waveCursor];
            for (int i = 0; i < wave.Cleared.Count; i++)
            {
                int index = IndexOf(wave.Cleared[i].Position);
                RectTransform root = _boardImages[index].transform.parent as RectTransform;
                if (root != null && !_anticipatedRoots.Contains(root)) _anticipatedRoots.Add(root);
            }
            if (wave.Index >= 1) ShowChain(wave.Index + 1);
            if (wave.Index == 1) ShowGlow(0.38f);
            if (wave.Index >= 2 && !_riotShownForAction)
            {
                _riotShownForAction = true;
                ShowRiot();
            }
        }

        private void TickAnticipation()
        {
            if (_reducedMotion) return;
            float t = Mathf.Clamp01(_phaseTime / AnticipationDuration);
            float scale = Mathf.Lerp(1f, 0.88f, SmoothStep(t));
            for (int i = 0; i < _anticipatedRoots.Count; i++)
                _anticipatedRoots[i].localScale = new Vector3(1.04f, scale, 1f);
        }

        private void ReleaseWave()
        {
            ResolutionWave wave = _resolution.Waves[_waveCursor];
            ResetAnticipation();
            int clearedCursor = 0;
            for (int group = 0; group < wave.GroupSizes.Count; group++)
            {
                int size = wave.GroupSizes[group];
                Vector2 centre = Vector2.zero;
                JellyColour colour = wave.Cleared[clearedCursor].Colour;
                for (int i = 0; i < size; i++) centre += BoardLocalToOverlay(wave.Cleared[clearedCursor + i].Position);
                centre /= Mathf.Max(1, size);
                int particles = _reducedMotion ? 2 : Mathf.Clamp(6 + (size / 2), 6, MaximumParticlesPerGroup);
                SpawnBurst(centre, ColourFor(colour), particles, wave.Index >= 2 || size >= 8);
                clearedCursor += size;
            }
            WaveScore score = ScoreForWave(_scoring, wave.Index);
            if (score.Points > 0L) SpawnScoreLabel(Centroid(wave), score.Points);
            _applyWave?.Invoke(wave);
            _phase = PresentationPhase.Pop;
            _phaseTime = 0f;
        }

        private void AdvanceAfterWave()
        {
            _waveCursor++;
            if (HasWave(_resolution, _waveCursor))
            {
                BeginWave();
                return;
            }
            if (!_showingPressureWaves && !_pressureApplied)
            {
                BeginRowRise();
                return;
            }
            CompletePresentation();
        }

        private void BeginRowRise()
        {
            _pressureApplied = true;
            _applyPressureRow?.Invoke();
            _boardRoot.anchoredPosition = _boardRestPosition + Vector2.down * 82f;
            ShowWarning(_library.RowRiseWarning, _reducedMotion ? 0.35f : 0.7f);
            _phase = PresentationPhase.RowRise;
            _phaseTime = 0f;
        }

        private void AdvanceAfterRowRise()
        {
            _boardRoot.anchoredPosition = _boardRestPosition;
            if (_action != null && _action.PressureResolution != null && _action.PressureResolution.WaveCount > 0)
            {
                _showingPressureWaves = true;
                _riotShownForAction = false;
                _resolution = _action.PressureResolution;
                _scoring = _action.PressureScoring;
                _waveCursor = 0;
                BeginWave();
                return;
            }
            CompletePresentation();
        }

        private void CompletePresentation()
        {
            Action callback = _finished;
            _phase = PresentationPhase.None;
            _action = null;
            _finished = null;
            callback?.Invoke();
        }

        private void SpawnBurst(Vector2 centre, Color tint, int count, bool expressive)
        {
            if (_library == null) return;
            Sprite splash = Pick(_library.Splashes);
            SpawnParticle(splash, centre, Vector2.zero, Color.Lerp(Color.white, tint, 0.28f),
                expressive ? 1.2f : 0.9f, expressive ? 1.55f : 1.22f, PopDuration, 0f);
            if (_reducedMotion) return;
            int available = ParticlePoolSize - ActiveParticleCount;
            count = Mathf.Min(count, available);
            for (int i = 0; i < count; i++)
            {
                float angle = ((Mathf.PI * 2f) * i / Mathf.Max(1, count)) + RandomRange(-0.22f, 0.22f);
                float speed = RandomRange(expressive ? 150f : 105f, expressive ? 260f : 195f);
                Vector2 velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                Sprite sprite = (i & 1) == 0 ? Pick(_library.Droplets) : Pick(_library.Sparkles);
                SpawnParticle(sprite, centre + RandomInside(12f), velocity, Color.Lerp(Color.white, tint, 0.48f),
                    RandomRange(0.12f, 0.25f), 0.04f, RandomRange(0.34f, 0.52f), RandomRange(-210f, 210f));
            }
        }

        private void SpawnParticle(Sprite sprite, Vector2 start, Vector2 velocity, Color colour,
            float startScale, float endScale, float lifetime, float rotationSpeed)
        {
            PooledSprite particle = FirstFreeParticle();
            if (particle == null || sprite == null) return;
            particle.Active = true;
            particle.Start = start;
            particle.Velocity = velocity;
            particle.Colour = colour;
            particle.StartScale = startScale;
            particle.EndScale = endScale;
            particle.StartRotation = RandomRange(-25f, 25f);
            particle.RotationSpeed = rotationSpeed;
            particle.Age = 0f;
            particle.Lifetime = lifetime;
            particle.Image.sprite = sprite;
            particle.Image.color = colour;
            particle.Rect.anchoredPosition = start;
            particle.Rect.localScale = Vector3.one * startScale;
            particle.Rect.localRotation = Quaternion.Euler(0f, 0f, particle.StartRotation);
            particle.Image.gameObject.SetActive(true);
            ActiveParticleCount++;
            PeakActiveParticleCount = Mathf.Max(PeakActiveParticleCount, ActiveParticleCount);
        }

        private void TickParticles(float deltaTime)
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                PooledSprite particle = _particles[i];
                if (particle == null || !particle.Active) continue;
                particle.Age += deltaTime;
                float t = Mathf.Clamp01(particle.Age / particle.Lifetime);
                Vector2 movement = particle.Velocity * particle.Age;
                movement.y -= 130f * particle.Age * particle.Age;
                particle.Rect.anchoredPosition = particle.Start + movement;
                particle.Rect.localScale = Vector3.one * Mathf.Lerp(particle.StartScale, particle.EndScale, t);
                particle.Rect.localRotation = Quaternion.Euler(0f, 0f,
                    particle.StartRotation + (particle.RotationSpeed * particle.Age));
                Color colour = particle.Colour;
                colour.a *= 1f - SmoothStep(t);
                particle.Image.color = colour;
                if (particle.Age >= particle.Lifetime) Deactivate(particle);
            }
        }

        private void SpawnScoreLabel(Vector2 start, long points)
        {
            PooledLabel label = FirstFreeLabel();
            if (label == null || points <= 0L) return;
            label.Active = true;
            label.Start = start + Vector2.up * 35f;
            label.Age = 0f;
            label.Lifetime = _reducedMotion ? 0.4f : 0.72f;
            label.Text.text = $"+{points:N0}";
            label.Text.color = Color.white;
            label.Rect.anchoredPosition = label.Start;
            label.Rect.localScale = Vector3.one * 0.78f;
            label.Text.gameObject.SetActive(true);
        }

        private void TickLabels(float deltaTime)
        {
            for (int i = 0; i < _labels.Length; i++)
            {
                PooledLabel label = _labels[i];
                if (label == null || !label.Active) continue;
                label.Age += deltaTime;
                float t = Mathf.Clamp01(label.Age / label.Lifetime);
                label.Rect.anchoredPosition = label.Start + Vector2.up * (_reducedMotion ? 18f : 105f) * t;
                label.Rect.localScale = Vector3.one * Mathf.Lerp(0.78f, 1.08f, Mathf.Sin(t * Mathf.PI));
                Color colour = Color.white;
                colour.a = 1f - SmoothStep(t);
                label.Text.color = colour;
                if (label.Age >= label.Lifetime) Deactivate(label);
            }
        }

        private void ShowChain(int multiplier)
        {
            _chainText.text = $"CHAIN x{multiplier}";
            _chainText.color = Color.white;
            _chainText.rectTransform.localScale = _reducedMotion ? Vector3.one : Vector3.one * 0.72f;
            _chainText.gameObject.SetActive(true);
            _chainText.rectTransform.SetAsLastSibling();
            _chainTime = _reducedMotion ? 0.5f : 0.62f;
        }

        private void ShowRiot()
        {
            _riot.color = Color.white;
            _riot.rectTransform.localScale = _reducedMotion ? Vector3.one : Vector3.one * 0.35f;
            _riot.gameObject.SetActive(true);
            _riot.rectTransform.SetAsLastSibling();
            _riotTime = RiotDuration;
            RiotRevealCount++;
            if (!_reducedMotion)
            {
                ShowGlow(0.56f);
                for (int i = 0; i < 6; i++)
                {
                    Sprite trail = Pick(_library.Trails);
                    SpawnParticle(trail, RandomInside(230f), RandomInside(55f), Color.white,
                        RandomRange(0.3f, 0.52f), RandomRange(0.45f, 0.7f), 0.62f, RandomRange(-35f, 35f));
                }
            }
        }

        private void ShowGlow(float duration)
        {
            if (_reducedMotion) return;
            _glow.sprite = Pick(_library.Glows);
            _glow.color = new Color(1f, 1f, 1f, 0f);
            _glow.gameObject.SetActive(true);
            _glowTime = duration;
        }

        private void ShowWarning(Sprite sprite, float duration)
        {
            _warning.sprite = sprite;
            _warning.color = Color.white;
            _warning.rectTransform.localScale = Vector3.one;
            _warning.gameObject.SetActive(true);
            _warning.rectTransform.SetAsLastSibling();
            _warningTime = duration;
        }

        private void HideWarning()
        {
            _warningTime = 0f;
            if (_warning != null) _warning.gameObject.SetActive(false);
        }

        private void TickPersistentEffects(float deltaTime)
        {
            if (_chainTime > 0f)
            {
                _chainTime -= deltaTime;
                float progress = 1f - Mathf.Clamp01(_chainTime / 0.62f);
                if (!_reducedMotion) _chainText.rectTransform.localScale = Vector3.one *
                    (progress < 0.2f ? Mathf.Lerp(0.72f, 1.08f, progress / 0.2f) : Mathf.Lerp(1.08f, 1f, progress));
                Color colour = Color.white;
                colour.a = _chainTime < 0.16f ? Mathf.Clamp01(_chainTime / 0.16f) : 1f;
                _chainText.color = colour;
                if (_chainTime <= 0f) _chainText.gameObject.SetActive(false);
            }
            if (_riotTime > 0f)
            {
                _riotTime -= deltaTime;
                float progress = 1f - Mathf.Clamp01(_riotTime / RiotDuration);
                if (!_reducedMotion)
                {
                    float scale = progress < 0.22f ? Mathf.Lerp(0.35f, 1.08f, EaseOutBack(progress / 0.22f)) :
                        progress < 0.72f ? Mathf.Lerp(1.08f, 1f, (progress - 0.22f) / 0.5f) :
                        Mathf.Lerp(1f, 0.82f, (progress - 0.72f) / 0.28f);
                    _riot.rectTransform.localScale = Vector3.one * scale;
                    float shake = Mathf.Sin(progress * 36f) * (1f - progress) * 5f;
                    _boardRoot.anchoredPosition = _boardRestPosition + Vector2.right * shake;
                }
                Color colour = Color.white;
                colour.a = progress < 0.75f ? 1f : 1f - ((progress - 0.75f) / 0.25f);
                _riot.color = colour;
                if (_riotTime <= 0f)
                {
                    _riot.gameObject.SetActive(false);
                    _boardRoot.anchoredPosition = _boardRestPosition;
                }
            }
            if (_glowTime > 0f)
            {
                _glowTime -= deltaTime;
                float alpha = Mathf.Sin(Mathf.Clamp01(_glowTime / 0.56f) * Mathf.PI) * 0.22f;
                _glow.color = new Color(1f, 1f, 1f, alpha);
                if (_glowTime <= 0f) _glow.gameObject.SetActive(false);
            }
            if (_warningTime > 0f)
            {
                _warningTime -= deltaTime;
                if (!_reducedMotion)
                {
                    float pulse = 1f + (Mathf.Sin(Time.unscaledTime * 11f) * 0.045f);
                    _warning.rectTransform.localScale = Vector3.one * pulse;
                }
                if (_warningTime <= 0f && !_oneMoveVisible) _warning.gameObject.SetActive(false);
            }
        }

        private void BuildPersistentViews()
        {
            _glow = CreateImage("Board Accent Glow", new Vector2(760f, 980f));
            _riot = CreateImage("RIOT Celebration", new Vector2(760f, 255f));
            _warning = CreateImage("Pressure Warning", new Vector2(470f, 235f));
            _warning.rectTransform.anchoredPosition = new Vector2(0f, 520f);
            _chainText = CreateText("Chain Label", 74f, new Vector2(720f, 125f));
            _chainText.rectTransform.anchoredPosition = new Vector2(0f, 450f);
            _riot.sprite = _library.Riot;
            _riot.raycastTarget = false;
        }

        private void BuildPools()
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                Image image = CreateImage($"VFX Sprite {i + 1}", new Vector2(190f, 190f));
                image.gameObject.SetActive(false);
                _particles[i] = new PooledSprite { Image = image, Rect = image.rectTransform };
            }
            for (int i = 0; i < _labels.Length; i++)
            {
                TextMeshProUGUI label = CreateText($"Score Fly-up {i + 1}", 42f, new Vector2(260f, 80f));
                label.gameObject.SetActive(false);
                _labels[i] = new PooledLabel { Text = label, Rect = label.rectTransform };
            }
        }

        private Image CreateImage(string name, Vector2 size)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(_overlay, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            Image image = gameObject.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private TextMeshProUGUI CreateText(string name, float size, Vector2 dimensions)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            gameObject.SetActive(false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(_overlay, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = dimensions;
            TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
            text.font = _font;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = size;
            text.fontStyle = FontStyles.Bold;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static TMP_FontAsset ResolveFont()
        {
            TMP_Settings settings = TMP_Settings.LoadDefaultSettings();
            if (settings != null)
            {
                TMP_FontAsset configured = TMP_Settings.GetFontAsset();
                if (configured != null) return configured;
            }
            Font legacy = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (legacy == null) throw new InvalidOperationException("A runtime font is required for VFX labels.");
            return TMP_FontAsset.CreateFontAsset(legacy);
        }

        private Vector2 BoardLocalToOverlay(BoardCoordinate coordinate)
        {
            int index = IndexOf(coordinate);
            Vector3 world = _boardImages[index].rectTransform.TransformPoint(Vector3.zero);
            return _overlay.InverseTransformPoint(world);
        }

        private Vector2 Centroid(ResolutionWave wave)
        {
            Vector2 centre = Vector2.zero;
            for (int i = 0; i < wave.Cleared.Count; i++) centre += BoardLocalToOverlay(wave.Cleared[i].Position);
            return wave.Cleared.Count == 0 ? Vector2.zero : centre / wave.Cleared.Count;
        }

        private void ResetAnticipation()
        {
            for (int i = 0; i < _anticipatedRoots.Count; i++)
                if (_anticipatedRoots[i] != null) _anticipatedRoots[i].localScale = Vector3.one;
            _anticipatedRoots.Clear();
        }

        private PooledSprite FirstFreeParticle()
        {
            for (int i = 0; i < _particles.Length; i++) if (_particles[i] != null && !_particles[i].Active) return _particles[i];
            return null;
        }

        private PooledLabel FirstFreeLabel()
        {
            for (int i = 0; i < _labels.Length; i++) if (_labels[i] != null && !_labels[i].Active) return _labels[i];
            return null;
        }

        private void Deactivate(PooledSprite particle)
        {
            if (particle == null || !particle.Active) return;
            particle.Active = false;
            particle.Image.gameObject.SetActive(false);
            ActiveParticleCount = Mathf.Max(0, ActiveParticleCount - 1);
        }

        private static void Deactivate(PooledLabel label)
        {
            if (label == null || !label.Active) return;
            label.Active = false;
            label.Text.gameObject.SetActive(false);
        }

        private Sprite Pick(Sprite[] sprites) => sprites[_cosmeticRandom.Next(sprites.Length)];
        private float RandomRange(float min, float max) => min + ((float)_cosmeticRandom.NextDouble() * (max - min));
        private Vector2 RandomInside(float radius)
        {
            float angle = RandomRange(0f, Mathf.PI * 2f);
            float distance = Mathf.Sqrt(RandomRange(0f, 1f)) * radius;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
        }

        private static bool HasWave(BoardResolutionResult result, int index) =>
            result != null && index >= 0 && index < result.Waves.Count;
        private static int IndexOf(BoardCoordinate coordinate) => (coordinate.Row * BoardModel.Width) + coordinate.Column;
        private static float SmoothStep(float t) => t * t * (3f - (2f * t));
        private static float EaseOutCubic(float t) { float p = 1f - t; return 1f - (p * p * p); }
        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            float p = t - 1f;
            return 1f + ((c1 + 1f) * p * p * p) + (c1 * p * p);
        }

        private static WaveScore ScoreForWave(ScoreResolutionResult scoring, int waveIndex)
        {
            if (scoring != null)
                for (int i = 0; i < scoring.Waves.Count; i++)
                    if (scoring.Waves[i].WaveIndex == waveIndex) return scoring.Waves[i];
            return default;
        }

        private static Color ColourFor(JellyColour colour)
        {
            switch (colour)
            {
                case JellyColour.Coral: return new Color32(255, 91, 117, 255);
                case JellyColour.Lemon: return new Color32(255, 220, 69, 255);
                case JellyColour.Aqua: return new Color32(69, 223, 245, 255);
                case JellyColour.Lilac: return new Color32(191, 126, 255, 255);
                case JellyColour.Mint: return new Color32(89, 235, 180, 255);
                default: return Color.white;
            }
        }
    }
}
