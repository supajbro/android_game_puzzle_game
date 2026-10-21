using System;
using JellyRiot.Core;
using UnityEngine;
using UnityEngine.UI;

namespace JellyRiot.Visuals
{
    /// <summary>
    /// Lightweight, reusable presentation for one board or rack jelly. It owns cosmetic animation
    /// state only; gameplay remains authoritative in Core and never reads this component.
    /// </summary>
    public sealed class AnimatedJellyView : MonoBehaviour
    {
        private enum ViewState { Hidden, Idle, Blink, Falling, Landing, Recovering }

        private const float IdleFrameDuration = 0.12f;
        private const float BlinkFrameDuration = 0.065f;
        private const float LandingFrameDuration = 0.045f;
        private const float RecoveryFrameDuration = 0.045f;
        private const float IdleCycleSeconds = 1.65f;

        private Image _image;
        private Image _shadow;
        private JellyAnimationLibrary _library;
        private Vector2 _restPosition;
        private Vector2 _imageSize;
        private Vector2 _shadowPosition;
        private Vector2 _shadowSize;
        private Sprite _shadowSprite;
        private Color _shadowColour;
        private System.Random _cosmeticRandom;
        private JellyColour _colour;
        private ViewState _state;
        private float _stateTime;
        private float _idlePhase;
        private float _nextBlink;
        private float _fallDuration;
        private float _fallStartY;
        private bool _reducedMotion;
        private bool _impactPending;

        public Image Image => _image;
        public JellyColour Colour => _colour;
        public bool IsVisible => _state != ViewState.Hidden && _image != null && _image.gameObject.activeSelf;
        public bool IsDropPlaying => _state == ViewState.Falling || _state == ViewState.Landing || _state == ViewState.Recovering;
        public Vector2 RestPosition => _restPosition;
        public Vector2 RootPosition => ((RectTransform)transform).anchoredPosition;
        public Vector2 Size => _imageSize;

        public void Initialise(Image image, Image shadow, JellyAnimationLibrary library, int cosmeticSeed)
        {
            _image = image != null ? image : throw new ArgumentNullException(nameof(image));
            _shadow = shadow;
            _library = library != null ? library : throw new ArgumentNullException(nameof(library));
            _restPosition = image.rectTransform.anchoredPosition;
            _imageSize = image.rectTransform.sizeDelta;
            _shadowPosition = shadow == null ? Vector2.zero : shadow.rectTransform.anchoredPosition;
            _shadowSize = shadow == null ? Vector2.zero : shadow.rectTransform.sizeDelta;
            _shadowSprite = shadow == null ? null : shadow.sprite;
            _shadowColour = shadow == null ? Color.white : shadow.color;
            _cosmeticRandom = new System.Random(cosmeticSeed);
            _idlePhase = (float)_cosmeticRandom.NextDouble() * IdleCycleSeconds;
            ScheduleCosmetics();
            ReturnToPool();
        }

        public void SetReducedMotion(bool reducedMotion)
        {
            _reducedMotion = reducedMotion;
            if (reducedMotion && _state == ViewState.Blink) EnterIdle(true);
        }

        public void SetColour(JellyColour colour, bool skipToSettled = false)
        {
            if (colour == JellyColour.Empty) throw new ArgumentOutOfRangeException(nameof(colour));
            _colour = colour;
            ResetVisuals();
            _image.gameObject.SetActive(true);
            if (_shadow != null) _shadow.gameObject.SetActive(true);
            EnterIdle(skipToSettled || _reducedMotion);
        }

        public void PlayDrop(JellyColour colour, float startY, float duration, bool reducedMotion)
        {
            SetReducedMotion(reducedMotion);
            SetColour(colour, true);
            _fallStartY = startY;
            _fallDuration = Mathf.Max(0.01f, duration);
            _stateTime = 0f;
            _impactPending = false;
            _state = ViewState.Falling;
            ApplyFallingPose(0f);
        }

        public bool ConsumeLandingImpact()
        {
            if (!_impactPending) return false;
            _impactPending = false;
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (_state == ViewState.Hidden || deltaTime <= 0f) return;
            _stateTime += deltaTime;
            switch (_state)
            {
                case ViewState.Falling: TickFalling(); break;
                case ViewState.Landing: TickSequence(JellyAnimationType.Landing, LandingFrameDuration, ViewState.Recovering); break;
                case ViewState.Recovering: TickRecovering(); break;
                case ViewState.Blink: TickSequence(JellyAnimationType.Blink, BlinkFrameDuration, ViewState.Idle); break;
                case ViewState.Idle: TickIdle(); break;
            }
        }

        public void SkipToSettled()
        {
            if (_colour == JellyColour.Empty) return;
            ResetVisuals();
            _image.gameObject.SetActive(true);
            if (_shadow != null) _shadow.gameObject.SetActive(true);
            EnterIdle(true);
        }

        public void ReturnToPool()
        {
            _colour = JellyColour.Empty;
            _state = ViewState.Hidden;
            _stateTime = 0f;
            _impactPending = false;
            ResetVisuals();
            _image.sprite = null;
            _image.gameObject.SetActive(false);
            if (_shadow != null) _shadow.gameObject.SetActive(false);
        }

        private void TickFalling()
        {
            float progress = Mathf.Clamp01(_stateTime / _fallDuration);
            ApplyFallingPose(progress);
            if (progress < 1f) return;
            _impactPending = true;
            _stateTime = 0f;
            if (_reducedMotion) EnterIdle(true);
            else _state = ViewState.Landing;
        }

        private void TickRecovering()
        {
            Sprite[] frames = _library.GetFrames(_colour, JellyAnimationType.RecoverToIdle);
            float progress = Mathf.Clamp01(_stateTime / (frames.Length * RecoveryFrameDuration));
            SetFrame(frames, progress);
            float arc = Mathf.Sin(progress * Mathf.PI);
            SetBottomAnchoredPose(1f - (0.018f * arc), 1f + (0.025f * arc), 8f * arc,
                1f - (0.045f * arc), 1f - (0.12f * arc));
            if (progress >= 1f) EnterIdle(false);
        }

        private void TickSequence(JellyAnimationType type, float frameDuration, ViewState nextState)
        {
            Sprite[] frames = _library.GetFrames(_colour, type);
            float progress = Mathf.Clamp01(_stateTime / (frames.Length * frameDuration));
            SetFrame(frames, progress);
            if (type == JellyAnimationType.Landing)
            {
                float squash = Mathf.Sin(progress * Mathf.PI);
                SetBottomAnchoredPose(1f + (0.05f * squash), 1f - (0.045f * squash), 0f,
                    1f + (0.09f * squash), 1f);
            }
            if (progress < 1f) return;
            _stateTime = 0f;
            if (nextState == ViewState.Idle) EnterIdle(false);
            else _state = nextState;
        }

        private void TickIdle()
        {
            Sprite[] idle = _library.GetFrames(_colour, JellyAnimationType.IdleLoop);
            int frame = Mathf.Abs(Mathf.FloorToInt((_stateTime + _idlePhase) / IdleFrameDuration)) % idle.Length;
            _image.sprite = idle[frame];
            float wave = Mathf.Sin(((_stateTime + _idlePhase) / IdleCycleSeconds) * Mathf.PI * 2f);
            float scaleY = 1f + (wave * 0.012f);
            float scaleX = 1f - (wave * 0.008f);
            SetBottomAnchoredPose(scaleX, scaleY, Mathf.Max(0f, wave) * 1.5f,
                1f - (Mathf.Max(0f, wave) * 0.015f), 1f);
            if (_reducedMotion) return;
            if (_stateTime >= _nextBlink) BeginCosmetic(ViewState.Blink);
        }

        private void BeginCosmetic(ViewState state)
        {
            _state = state;
            _stateTime = 0f;
        }

        private void EnterIdle(bool settledOnly)
        {
            ResetVisuals();
            _image.sprite = _library.GetSettledFrame(_colour);
            _state = ViewState.Idle;
            _stateTime = settledOnly ? -_idlePhase : 0f;
            ScheduleCosmetics();
        }

        private void ScheduleCosmetics()
        {
            if (_cosmeticRandom == null) return;
            _nextBlink = 3.5f + ((float)_cosmeticRandom.NextDouble() * 6.5f);
        }

        private void ApplyFallingPose(float progress)
        {
            _image.sprite = _library.GetSettledFrame(_colour);
            float accelerated = progress * progress;
            float y = Mathf.LerpUnclamped(_fallStartY, _restPosition.y, accelerated);
            float stretch = _reducedMotion ? 0f : Mathf.Sin(progress * Mathf.PI) * 0.025f;
            _image.rectTransform.anchoredPosition = new Vector2(_restPosition.x, y);
            _image.rectTransform.localScale = new Vector3(1f - stretch, 1f + stretch, 1f);
            SetShadowPose(Mathf.Lerp(0.72f, 1f, accelerated), Mathf.Lerp(0.5f, 1f, accelerated));
        }

        private void SetFrame(Sprite[] frames, float progress)
        {
            int index = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(Mathf.Clamp01(progress) * frames.Length));
            _image.sprite = frames[index];
        }

        private void SetBottomAnchoredPose(float scaleX, float scaleY, float bounceHeight,
            float shadowScale, float shadowAlpha)
        {
            float anchoredOffset = (_imageSize.y * (scaleY - 1f) * 0.5f) + bounceHeight;
            _image.rectTransform.anchoredPosition = _restPosition + new Vector2(0f, anchoredOffset);
            _image.rectTransform.localScale = new Vector3(scaleX, scaleY, 1f);
            SetShadowPose(shadowScale, shadowAlpha);
        }

        private void SetShadowPose(float scale, float alpha)
        {
            if (_shadow == null) return;
            _shadow.rectTransform.localScale = new Vector3(scale, scale, 1f);
            Color colour = _shadowColour;
            colour.a *= Mathf.Clamp01(alpha);
            _shadow.color = colour;
            _shadow.canvasRenderer.SetAlpha(1f);
        }

        private void ResetVisuals()
        {
            ResetTransform(_image, _restPosition, _imageSize, Color.white);
            if (_shadow == null) return;
            ResetTransform(_shadow, _shadowPosition, _shadowSize, _shadowColour);
            _shadow.sprite = _shadowSprite;
        }

        private static void ResetTransform(Image image, Vector2 position, Vector2 size, Color colour)
        {
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = size;
            image.rectTransform.localRotation = Quaternion.identity;
            image.rectTransform.localScale = Vector3.one;
            image.color = colour;
            image.canvasRenderer.SetAlpha(1f);
        }
    }
}
