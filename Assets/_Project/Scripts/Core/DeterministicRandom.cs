using System;

namespace JellyRiot.Core
{
    /// <summary>
    /// Small PCG-XSH-RR generator. Its algorithm and state shape are fixed so saved runs remain reproducible.
    /// </summary>
    public sealed class DeterministicRandom : IGameRandom
    {
        private const ulong DefaultStream = 1442695040888963407UL;
        private const ulong Multiplier = 6364136223846793005UL;

        private ulong _state;
        private ulong _increment;

        public DeterministicRandom(ulong seed)
        {
            _increment = DefaultStream | 1UL;
            _state = 0UL;
            NextUInt();
            _state = unchecked(_state + MixSeed(seed));
            NextUInt();
        }

        public DeterministicRandom(DeterministicRandomState state)
        {
            Restore(state);
        }

        public DeterministicRandomState State => new DeterministicRandomState(_state, _increment);

        public uint NextUInt()
        {
            ulong oldState = _state;
            _state = unchecked((oldState * Multiplier) + _increment);
            uint value = (uint)(((oldState >> 18) ^ oldState) >> 27);
            int rotation = (int)(oldState >> 59);
            return (value >> rotation) | (value << ((-rotation) & 31));
        }

        public int NextInt(int maximumExclusive)
        {
            if (maximumExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumExclusive), "Maximum must be positive.");
            }

            uint bound = (uint)maximumExclusive;
            uint threshold = unchecked((uint)(0U - bound)) % bound;
            while (true)
            {
                uint value = NextUInt();
                if (value >= threshold)
                {
                    return (int)(value % bound);
                }
            }
        }

        public void Restore(DeterministicRandomState state)
        {
            if ((state.Increment & 1UL) == 0UL)
            {
                throw new ArgumentException("A deterministic random stream increment must be odd.", nameof(state));
            }

            _state = state.State;
            _increment = state.Increment;
        }

        private static ulong MixSeed(ulong value)
        {
            value = unchecked(value + 0x9E3779B97F4A7C15UL);
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }
}
