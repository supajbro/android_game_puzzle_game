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

        [Header("Pressure tuning")]
        [SerializeField, Min(1), Tooltip("Valid placements before the first pressure row.")]
        private int firstPressureTurn = 6;

        [SerializeField, Min(1)] private int earlyPressureInterval = 6;
        [SerializeField, Min(1)] private int middlePressureInterval = 5;
        [SerializeField, Min(1)] private int latePressureInterval = 4;

        [SerializeField, Min(2)] private int middlePressureTurn = 20;
        [SerializeField, Min(3)] private int latePressureTurn = 45;
        [SerializeField, Range(3, 5)] private int minimumPressureFilled = 3;
        [SerializeField, Range(3, 5)] private int maximumPressureFilled = 5;

        [Header("Colour progression and generation")]
        [SerializeField, Min(1)] private int fourthColourTurn = 20;
        [SerializeField, Min(2)] private int fifthColourTurn = 50;
        [SerializeField] private bool useWeightedGeneration = true;
        [SerializeField, Min(1), Tooltip("Safety budget for retryable initial-board generation.")]
        private int initialGenerationAttemptLimit = 32;

        public int FirstPressureTurn => firstPressureTurn;
        public int EarlyPressureInterval => earlyPressureInterval;
        public int MiddlePressureInterval => middlePressureInterval;
        public int LatePressureInterval => latePressureInterval;

        public GameBalance ToCoreBalance() => new GameBalance(initialActiveColours, initialOccupiedCells,
            firstPressureTurn, earlyPressureInterval, middlePressureInterval, latePressureInterval,
            middlePressureTurn, latePressureTurn, minimumPressureFilled, maximumPressureFilled,
            fourthColourTurn, fifthColourTurn, useWeightedGeneration, initialGenerationAttemptLimit);

        private void OnValidate()
        {
            initialActiveColours = Mathf.Clamp(initialActiveColours, 3, 5);
            initialOccupiedCells = Mathf.Clamp(initialOccupiedCells, 16, 22);
            firstPressureTurn = Mathf.Max(1, firstPressureTurn);
            earlyPressureInterval = Mathf.Max(1, earlyPressureInterval);
            middlePressureInterval = Mathf.Max(1, middlePressureInterval);
            latePressureInterval = Mathf.Max(1, latePressureInterval);
            middlePressureTurn = Mathf.Max(firstPressureTurn + 1, middlePressureTurn);
            latePressureTurn = Mathf.Max(middlePressureTurn + 1, latePressureTurn);
            minimumPressureFilled = Mathf.Clamp(minimumPressureFilled, 3, 5);
            maximumPressureFilled = Mathf.Clamp(maximumPressureFilled, minimumPressureFilled, 5);
            fourthColourTurn = Mathf.Max(1, fourthColourTurn);
            fifthColourTurn = Mathf.Max(fourthColourTurn + 1, fifthColourTurn);
            initialGenerationAttemptLimit = Mathf.Max(1, initialGenerationAttemptLimit);
        }
    }
}
