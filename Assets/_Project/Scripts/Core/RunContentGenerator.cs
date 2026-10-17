using System;

namespace JellyRiot.Core
{
    [Serializable]
    public sealed class ContentGeneratorSnapshot
    {
        public DeterministicRandomState RandomState;
        public JellyColour PreviousColour;
        public int RepeatCount;
    }

    /// <summary>One deterministic, serialisable stream for rack candidates and pressure rows.</summary>
    public sealed class RunContentGenerator
    {
        private const ulong StreamSalt = 0x4A454C4C5952494FUL;
        private DeterministicRandom _random;
        private JellyColour _previousColour;
        private int _repeatCount;

        public RunContentGenerator(ulong runSeed)
        {
            _random = new DeterministicRandom(runSeed ^ StreamSalt);
        }

        public JellyColour NextColour(int activeColourCount, bool weighted)
        {
            if (activeColourCount < GameBalance.MinimumActiveColours ||
                activeColourCount > GameBalance.MaximumActiveColours)
            {
                throw new ArgumentOutOfRangeException(nameof(activeColourCount));
            }

            int repeatedWeight = weighted && _repeatCount >= 2 ? 1 : 3;
            int totalWeight = 0;
            for (int index = 0; index < activeColourCount; index++)
            {
                JellyColour colour = (JellyColour)(index + 1);
                totalWeight += colour == _previousColour ? repeatedWeight : 3;
            }

            int roll = _random.NextInt(totalWeight);
            int selected = 0;
            while (selected < activeColourCount)
            {
                JellyColour candidate = (JellyColour)(selected + 1);
                int weight = candidate == _previousColour ? repeatedWeight : 3;
                if (roll < weight)
                {
                    break;
                }

                roll -= weight;
                selected++;
            }

            JellyColour result = (JellyColour)(selected + 1);
            if (result == _previousColour)
            {
                _repeatCount++;
            }
            else
            {
                _previousColour = result;
                _repeatCount = 1;
            }

            return result;
        }

        public JellyColour[] NextPressureRow(GameBalance balance, int activeColourCount)
        {
            if (balance == null)
            {
                throw new ArgumentNullException(nameof(balance));
            }

            int filledCount = balance.MinimumPressureFilled +
                              _random.NextInt(balance.MaximumPressureFilled - balance.MinimumPressureFilled + 1);
            var row = new JellyColour[BoardModel.Width];
            var positions = new int[BoardModel.Width];
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] = i;
            }

            for (int i = 0; i < filledCount; i++)
            {
                int swapIndex = i + _random.NextInt(BoardModel.Width - i);
                int temporary = positions[i];
                positions[i] = positions[swapIndex];
                positions[swapIndex] = temporary;
                row[positions[i]] = NextColour(activeColourCount, balance.UseWeightedGeneration);
            }

            return row;
        }

        public ContentGeneratorSnapshot CreateSnapshot() => new ContentGeneratorSnapshot
        {
            RandomState = _random.State,
            PreviousColour = _previousColour,
            RepeatCount = _repeatCount
        };

        public void Restore(ContentGeneratorSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (snapshot.PreviousColour < JellyColour.Empty ||
                snapshot.PreviousColour > JellyColour.Mint || snapshot.RepeatCount < 0 ||
                (snapshot.PreviousColour == JellyColour.Empty && snapshot.RepeatCount != 0) ||
                (snapshot.PreviousColour != JellyColour.Empty && snapshot.RepeatCount == 0))
            {
                throw new ArgumentException("Generator snapshot is invalid.", nameof(snapshot));
            }

            _random = new DeterministicRandom(snapshot.RandomState);
            _previousColour = snapshot.PreviousColour;
            _repeatCount = snapshot.RepeatCount;
        }
    }
}
