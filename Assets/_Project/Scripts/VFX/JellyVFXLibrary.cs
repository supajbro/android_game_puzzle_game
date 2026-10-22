using System;
using UnityEngine;

namespace JellyRiot.VFX
{
    /// <summary>Inspector-facing references for the original Jelly Riot VFX artwork.</summary>
    [CreateAssetMenu(fileName = "JellyVFXLibrary", menuName = "Jelly Riot/VFX Library")]
    public sealed class JellyVFXLibrary : ScriptableObject
    {
        [SerializeField] private Sprite[] splashes = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] droplets = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] sparkles = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] trails = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] glows = Array.Empty<Sprite>();
        [SerializeField] private Sprite riot;
        [SerializeField] private Sprite oneMoveWarning;
        [SerializeField] private Sprite rowRiseWarning;

        public Sprite[] Splashes => splashes;
        public Sprite[] Droplets => droplets;
        public Sprite[] Sparkles => sparkles;
        public Sprite[] Trails => trails;
        public Sprite[] Glows => glows;
        public Sprite Riot => riot;
        public Sprite OneMoveWarning => oneMoveWarning;
        public Sprite RowRiseWarning => rowRiseWarning;

        public bool IsComplete => Has(splashes) && Has(droplets) && Has(sparkles) && Has(trails) &&
                                  Has(glows) && riot != null && oneMoveWarning != null && rowRiseWarning != null;

#if UNITY_EDITOR
        public void Configure(Sprite[] splashSprites, Sprite[] dropletSprites, Sprite[] sparkleSprites,
            Sprite[] trailSprites, Sprite[] glowSprites, Sprite riotSprite, Sprite oneMoveSprite,
            Sprite rowRiseSprite)
        {
            splashes = Copy(splashSprites, nameof(splashSprites));
            droplets = Copy(dropletSprites, nameof(dropletSprites));
            sparkles = Copy(sparkleSprites, nameof(sparkleSprites));
            trails = Copy(trailSprites, nameof(trailSprites));
            glows = Copy(glowSprites, nameof(glowSprites));
            riot = riotSprite;
            oneMoveWarning = oneMoveSprite;
            rowRiseWarning = rowRiseSprite;
            if (!IsComplete) throw new ArgumentException("Every VFX library category requires artwork.");
        }

        private static Sprite[] Copy(Sprite[] source, string name)
        {
            if (!Has(source)) throw new ArgumentException("At least one non-null sprite is required.", name);
            return (Sprite[])source.Clone();
        }
#endif

        private static bool Has(Sprite[] sprites)
        {
            if (sprites == null || sprites.Length == 0) return false;
            for (int i = 0; i < sprites.Length; i++) if (sprites[i] == null) return false;
            return true;
        }
    }
}
