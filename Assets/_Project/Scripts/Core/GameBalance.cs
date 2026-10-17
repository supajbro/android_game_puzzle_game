using System;

namespace JellyRiot.Core
{
    [Serializable]
    public sealed class GameBalance
    {
        public const int MinimumActiveColours = 3;
        public const int MaximumActiveColours = 5;

        public GameBalance(int initialActiveColours = 3, int initialOccupiedCells = 20)
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

            InitialActiveColours = initialActiveColours;
            InitialOccupiedCells = initialOccupiedCells;
        }

        public int InitialActiveColours { get; }

        public int InitialOccupiedCells { get; }

        public static GameBalance Default { get; } = new GameBalance();
    }
}
