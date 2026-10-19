using System;
using JellyRiot.Core;
using UnityEngine;

namespace JellyRiot.Visuals
{
    public enum JellyAnimationType
    {
        Falling = 0,
        Landing = 1,
        SquashStretch = 2,
        Blink = 3
    }

    [Serializable]
    public sealed class JellyAnimationSet
    {
        [SerializeField] private JellyColour colour;
        [SerializeField] private Sprite staticSprite;
        [SerializeField] private Sprite[] falling = new Sprite[5];
        [SerializeField] private Sprite[] landing = new Sprite[5];
        [SerializeField] private Sprite[] squashStretch = new Sprite[5];
        [SerializeField] private Sprite[] blink = new Sprite[5];

        public JellyColour Colour => colour;
        public Sprite StaticSprite => staticSprite;

        public Sprite[] GetFrames(JellyAnimationType type)
        {
            switch (type)
            {
                case JellyAnimationType.Falling: return falling;
                case JellyAnimationType.Landing: return landing;
                case JellyAnimationType.SquashStretch: return squashStretch;
                case JellyAnimationType.Blink: return blink;
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

#if UNITY_EDITOR
        public void Configure(JellyColour value, Sprite still, Sprite[] fallingFrames,
            Sprite[] landingFrames, Sprite[] squashFrames, Sprite[] blinkFrames)
        {
            colour = value;
            staticSprite = still;
            falling = Validate(fallingFrames, nameof(fallingFrames));
            landing = Validate(landingFrames, nameof(landingFrames));
            squashStretch = Validate(squashFrames, nameof(squashFrames));
            blink = Validate(blinkFrames, nameof(blinkFrames));
        }

        private static Sprite[] Validate(Sprite[] frames, string parameterName)
        {
            if (frames == null || frames.Length != 5)
            {
                throw new ArgumentException("An animation category must contain exactly five frames.", parameterName);
            }

            return (Sprite[])frames.Clone();
        }
#endif
    }

    /// <summary>
    /// Inspector-facing frame lookup only. Animation timing remains outside this Work Order.
    /// Static gameplay sprites stay authoritative whenever no presentation sequence is active.
    /// </summary>
    [CreateAssetMenu(menuName = "Jelly Riot/Visuals/Jelly Animation Library")]
    public sealed class JellyAnimationLibrary : ScriptableObject
    {
        [SerializeField] private JellyAnimationSet[] sets = new JellyAnimationSet[5];

        public Sprite GetStaticSprite(JellyColour colour) => Find(colour).StaticSprite;

        public Sprite[] GetFrames(JellyColour colour, JellyAnimationType type) =>
            Find(colour).GetFrames(type);

        private JellyAnimationSet Find(JellyColour colour)
        {
            if (colour == JellyColour.Empty)
            {
                throw new ArgumentOutOfRangeException(nameof(colour));
            }

            for (int i = 0; i < sets.Length; i++)
            {
                if (sets[i] != null && sets[i].Colour == colour)
                {
                    return sets[i];
                }
            }

            throw new InvalidOperationException($"No animation set is configured for {colour}.");
        }

#if UNITY_EDITOR
        public void Configure(JellyAnimationSet[] animationSets)
        {
            if (animationSets == null || animationSets.Length != 5)
            {
                throw new ArgumentException("The library requires exactly five colour sets.", nameof(animationSets));
            }

            sets = (JellyAnimationSet[])animationSets.Clone();
        }
#endif
    }
}
