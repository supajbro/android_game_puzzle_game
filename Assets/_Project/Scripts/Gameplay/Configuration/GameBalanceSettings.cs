using JellyRiot.Core;
using UnityEngine;

namespace JellyRiot.Gameplay.Configuration
{
    [CreateAssetMenu(fileName = "GameBalanceSettings", menuName = "Jelly Riot/Game Balance Settings")]
    public sealed class GameBalanceSettings : ScriptableObject
    {
        [SerializeField, Range(3, 5), Tooltip("Colours available when a new run begins.")]
        private int initialActiveColours = 3;

        [SerializeField, Range(16, 22), Tooltip("Occupied cells generated on the initial 7x9 board.")]
        private int initialOccupiedCells = 20;

        [Header("Future pressure tuning")]
        [SerializeField, Min(1), Tooltip("Valid placements before the first pressure row. Used from Work Order 05.")]
        private int firstPressureTurn = 6;

        [SerializeField, Min(1)] private int earlyPressureInterval = 6;
        [SerializeField, Min(1)] private int middlePressureInterval = 5;
        [SerializeField, Min(1)] private int latePressureInterval = 4;

        public int FirstPressureTurn => firstPressureTurn;
        public int EarlyPressureInterval => earlyPressureInterval;
        public int MiddlePressureInterval => middlePressureInterval;
        public int LatePressureInterval => latePressureInterval;

        public GameBalance ToCoreBalance() => new GameBalance(initialActiveColours, initialOccupiedCells);

        private void OnValidate()
        {
            initialActiveColours = Mathf.Clamp(initialActiveColours, 3, 5);
            initialOccupiedCells = Mathf.Clamp(initialOccupiedCells, 16, 22);
            firstPressureTurn = Mathf.Max(1, firstPressureTurn);
            earlyPressureInterval = Mathf.Max(1, earlyPressureInterval);
            middlePressureInterval = Mathf.Max(1, middlePressureInterval);
            latePressureInterval = Mathf.Max(1, latePressureInterval);
        }
    }
}
