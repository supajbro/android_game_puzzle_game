using System;

namespace JellyRiot.Core
{
    [Serializable]
    public struct DeterministicRandomState : IEquatable<DeterministicRandomState>
    {
        public DeterministicRandomState(ulong state, ulong increment)
        {
            State = state;
            Increment = increment;
        }

        public ulong State;

        public ulong Increment;

        public bool Equals(DeterministicRandomState other) => State == other.State && Increment == other.Increment;

        public override bool Equals(object obj) => obj is DeterministicRandomState other && Equals(other);

        public override int GetHashCode() => unchecked((State.GetHashCode() * 397) ^ Increment.GetHashCode());
    }
}
