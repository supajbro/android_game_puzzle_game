using System;

namespace JellyRiot.Core
{
    [Serializable]
    public readonly struct BoardCoordinate : IEquatable<BoardCoordinate>
    {
        public BoardCoordinate(int column, int row)
        {
            Column = column;
            Row = row;
        }

        public int Column { get; }

        public int Row { get; }

        public bool Equals(BoardCoordinate other) => Column == other.Column && Row == other.Row;

        public override bool Equals(object obj) => obj is BoardCoordinate other && Equals(other);

        public override int GetHashCode() => unchecked((Column * 397) ^ Row);

        public override string ToString() => $"({Column},{Row})";
    }
}
