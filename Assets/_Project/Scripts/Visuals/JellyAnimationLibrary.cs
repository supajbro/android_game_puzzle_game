using System;
using JellyRiot.Core;
using UnityEngine;

namespace JellyRiot.Visuals
{
    public enum JellyAnimationType
    {
        IdleLoop = 0,
        Blink = 1,
        Landing = 2,
        RecoverToIdle = 3
    }

    [Serializable]
    public sealed class JellyAnimationSet
    {
        [SerializeField] private JellyColour colour;
        [SerializeField] private Sprite[] idleLoop = new Sprite[10];
        [SerializeField] private Sprite[] blink = new Sprite[10];
        [SerializeField] private Sprite[] landing = new Sprite[10];
        [SerializeField] private Sprite[] recoverToIdle = new Sprite[10];

        public JellyColour Colour => colour;
        public Sprite SettledFrame => idleLoop != null && idleLoop.Length > 0 ? idleLoop[0] : null;

        public Sprite[] GetFrames(JellyAnimationType type)
        {
            switch (type)
            {
                case JellyAnimationType.IdleLoop: return idleLoop;
                case JellyAnimationType.Blink: return blink;
                case JellyAnimationType.Landing: return landing;
                case JellyAnimationType.RecoverToIdle: return recoverToIdle;
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

#if UNITY_EDITOR
        public void Configure(JellyColour value, Sprite[] idleFrames, Sprite[] blinkFrames,
            Sprite[] landingFrames, Sprite[] recoveryFrames)
        {
            colour = value;
            idleLoop = Validate(idleFrames, nameof(idleFrames));
            blink = Validate(blinkFrames, nameof(blinkFrames));
            landing = Validate(landingFrames, nameof(landingFrames));
            recoverToIdle = Validate(recoveryFrames, nameof(recoveryFrames));
        }

        private static Sprite[] Validate(Sprite[] frames, string parameterName)
        {
            if (frames == null || frames.Length != 10)
                throw new ArgumentException("An animation group must contain exactly ten frames.", parameterName);
            for (int i = 0; i < frames.Length; i++)
                if (frames[i] == null)
                    throw new ArgumentException("Animation groups cannot contain missing frames.", parameterName);
            return (Sprite[])frames.Clone();
        }
#endif
    }

    /// <summary>
    /// Inspector-facing lookup for the expressive 10 x 4 sheets. Every visible jelly frame comes
    /// from this library; the older standalone colour sprites are retained only as source backups.
    /// </summary>
    [CreateAssetMenu(menuName = "Jelly Riot/Visuals/Jelly Animation Library")]
    public sealed class JellyAnimationLibrary : ScriptableObject
    {
        [SerializeField] private JellyAnimationSet[] sets = new JellyAnimationSet[5];

        public Sprite GetSettledFrame(JellyColour colour) => Find(colour).SettledFrame;

        public Sprite[] GetFrames(JellyColour colour, JellyAnimationType type) =>
            Find(colour).GetFrames(type);

        public bool IsComplete()
        {
            if (sets == null || sets.Length != 5) return false;
            try
            {
                for (JellyColour colour = JellyColour.Coral; colour <= JellyColour.Mint; colour++)
                foreach (JellyAnimationType type in Enum.GetValues(typeof(JellyAnimationType)))
                {
                    Sprite[] frames = GetFrames(colour, type);
                    if (frames == null || frames.Length != 10) return false;
                    for (int i = 0; i < frames.Length; i++) if (frames[i] == null) return false;
                }
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            return true;
        }

        private JellyAnimationSet Find(JellyColour colour)
        {
            if (colour == JellyColour.Empty) throw new ArgumentOutOfRangeException(nameof(colour));
            for (int i = 0; i < sets.Length; i++)
                if (sets[i] != null && sets[i].Colour == colour) return sets[i];
            throw new InvalidOperationException($"No animation set is configured for {colour}.");
        }

#if UNITY_EDITOR
        public void Configure(JellyAnimationSet[] animationSets)
        {
            if (animationSets == null || animationSets.Length != 5)
                throw new ArgumentException("The library requires exactly five colour sets.", nameof(animationSets));
            sets = (JellyAnimationSet[])animationSets.Clone();
        }
#endif
    }
}
