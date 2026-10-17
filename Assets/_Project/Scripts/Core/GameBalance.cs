using System;

namespace JellyRiot.Core
{
    [Serializable]
    public sealed class GameBalance
    {
        public const int MinimumActiveColours = 3;
        public const int MaximumActiveColours = 5;

        public GameBalance(int initialActiveColours = 3, int initialOccupiedCells = 20,
            int firstPressureTurn = 6, int earlyPressureInterval = 6,
            int middlePressureInterval = 5, int latePressureInterval = 4,
            int middlePressureTurn = 20, int latePressureTurn = 45,
            int minimumPressureFilled = 3, int maximumPressureFilled = 5,
            int fourthColourTurn = 20, int fifthColourTurn = 50,
            bool useWeightedGeneration = true, int initialGenerationAttemptLimit = 32)
        {
            if (initialActiveColours < MinimumActiveColours || initialActiveColours > MaximumActiveColours)
            {
                throw new ArgumentOutOfRangeException(nameof(initialActiveColours));
            }

            int minimumOccupied = (BoardModel.CellCount * 25 + 99) / 100;
            int maximumOccupied = BoardModel.CellCount * 35 / 100;
            if (initialOccupiedCells < minimumOccupied || initialOccupiedCells > maximumOccupied)
            {
                throw new ArgumentOutOfRangeException(nameof(initialOccupiedCells));
            }

            if (firstPressureTurn < 1 || earlyPressureInterval < 1 || middlePressureInterval < 1 ||
                latePressureInterval < 1 || middlePressureTurn <= firstPressureTurn ||
                latePressureTurn <= middlePressureTurn)
            {
                throw new ArgumentOutOfRangeException(nameof(firstPressureTurn));
            }

            if (minimumPressureFilled < 3 || maximumPressureFilled > 5 ||
                minimumPressureFilled > maximumPressureFilled)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumPressureFilled));
            }

            if (fourthColourTurn < 1 || fifthColourTurn <= fourthColourTurn ||
                initialGenerationAttemptLimit < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(fourthColourTurn));
            }

            InitialActiveColours = initialActiveColours;
            InitialOccupiedCells = initialOccupiedCells;
            FirstPressureTurn = firstPressureTurn;
            EarlyPressureInterval = earlyPressureInterval;
            MiddlePressureInterval = middlePressureInterval;
            LatePressureInterval = latePressureInterval;
            MiddlePressureTurn = middlePressureTurn;
            LatePressureTurn = latePressureTurn;
            MinimumPressureFilled = minimumPressureFilled;
            MaximumPressureFilled = maximumPressureFilled;
            FourthColourTurn = fourthColourTurn;
            FifthColourTurn = fifthColourTurn;
            UseWeightedGeneration = useWeightedGeneration;
            InitialGenerationAttemptLimit = initialGenerationAttemptLimit;
        }

        public int InitialActiveColours { get; }

        public int InitialOccupiedCells { get; }

        public int FirstPressureTurn { get; }
        public int EarlyPressureInterval { get; }
        public int MiddlePressureInterval { get; }
        public int LatePressureInterval { get; }
        public int MiddlePressureTurn { get; }
        public int LatePressureTurn { get; }
        public int MinimumPressureFilled { get; }
        public int MaximumPressureFilled { get; }
        public int FourthColourTurn { get; }
        public int FifthColourTurn { get; }
        public bool UseWeightedGeneration { get; }
        public int InitialGenerationAttemptLimit { get; }

        public int GetActiveColourCount(int turnCount)
        {
            if (turnCount >= FifthColourTurn)
            {
                return MaximumActiveColours;
            }

            if (turnCount >= FourthColourTurn)
            {
                return Math.Max(InitialActiveColours, 4);
            }

            return InitialActiveColours;
        }

        public int GetPressureIntervalAfter(int completedTurn)
        {
            if (completedTurn + EarlyPressureInterval >= MiddlePressureTurn)
            {
                if (completedTurn + MiddlePressureInterval >= LatePressureTurn)
                {
                    return LatePressureInterval;
                }

                return MiddlePressureInterval;
            }

            return EarlyPressureInterval;
        }

        public static GameBalance Default { get; } = new GameBalance();
    }
}
