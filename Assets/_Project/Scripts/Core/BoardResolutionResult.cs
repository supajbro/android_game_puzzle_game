using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JellyRiot.Core
{
    [Serializable]
    public readonly struct ClearedJelly : IEquatable<ClearedJelly>
    {
        public ClearedJelly(BoardCoordinate position, JellyColour colour)
        {
            Position = position;
            Colour = colour;
        }

        public BoardCoordinate Position { get; }

        public JellyColour Colour { get; }

        public bool Equals(ClearedJelly other) => Position.Equals(other.Position) && Colour == other.Colour;

        public override bool Equals(object obj) => obj is ClearedJelly other && Equals(other);

        public override int GetHashCode() => unchecked((Position.GetHashCode() * 397) ^ (int)Colour);
    }

    [Serializable]
    public readonly struct FallingJelly : IEquatable<FallingJelly>
    {
        public FallingJelly(BoardCoordinate from, BoardCoordinate to, JellyColour colour)
        {
            From = from;
            To = to;
            Colour = colour;
        }

        public BoardCoordinate From { get; }

        public BoardCoordinate To { get; }

        public JellyColour Colour { get; }

        public bool Equals(FallingJelly other) =>
            From.Equals(other.From) && To.Equals(other.To) && Colour == other.Colour;

        public override bool Equals(object obj) => obj is FallingJelly other && Equals(other);

        public override int GetHashCode() =>
            unchecked((((From.GetHashCode() * 397) ^ To.GetHashCode()) * 397) ^ (int)Colour);
    }

    [Serializable]
    public sealed class ResolutionWave
    {
        internal ResolutionWave(int index, IList<ClearedJelly> cleared, IList<FallingJelly> falls)
        {
            Index = index;
            Cleared = new ReadOnlyCollection<ClearedJelly>(new List<ClearedJelly>(cleared));
            Falls = new ReadOnlyCollection<FallingJelly>(new List<FallingJelly>(falls));
        }

        public int Index { get; }

        public int ClearedCount => Cleared.Count;

        public IReadOnlyList<ClearedJelly> Cleared { get; }

        public IReadOnlyList<FallingJelly> Falls { get; }
    }

    [Serializable]
    public sealed class BoardResolutionResult
    {
        private static readonly BoardResolutionResult EmptyResult =
            new BoardResolutionResult(Array.Empty<ResolutionWave>());

        internal BoardResolutionResult(IList<ResolutionWave> waves)
        {
            Waves = new ReadOnlyCollection<ResolutionWave>(new List<ResolutionWave>(waves));
        }

        public IReadOnlyList<ResolutionWave> Waves { get; }

        public int WaveCount => Waves.Count;

        public int TotalClearedCount
        {
            get
            {
                int total = 0;
                for (int i = 0; i < Waves.Count; i++)
                {
                    total += Waves[i].ClearedCount;
                }

                return total;
            }
        }

        internal static BoardResolutionResult Empty => EmptyResult;
    }
}
