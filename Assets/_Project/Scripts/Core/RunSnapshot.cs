using System;

namespace JellyRiot.Core
{
    /// <summary>A deep-copied, serialisable boundary object for a settled deterministic run.</summary>
    [Serializable]
    public sealed class RunSnapshot
    {
        public RunSnapshot(ulong seed, int turnCount, int activeColourCount,
            DeterministicRandomState randomState, JellyColour[] cells)
        {
            Seed = seed;
            TurnCount = turnCount;
            ActiveColourCount = activeColourCount;
            RandomState = randomState;
            Cells = cells == null ? throw new ArgumentNullException(nameof(cells)) : (JellyColour[])cells.Clone();
        }

        public ulong Seed;

        public int TurnCount;

        public int ActiveColourCount;

        public DeterministicRandomState RandomState;

        public JellyColour[] Cells;

        public JellyColour[] CopyCells() => (JellyColour[])Cells.Clone();
    }
}
