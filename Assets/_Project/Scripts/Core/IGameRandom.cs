namespace JellyRiot.Core
{
    public interface IGameRandom
    {
        DeterministicRandomState State { get; }

        uint NextUInt();

        int NextInt(int maximumExclusive);

        void Restore(DeterministicRandomState state);
    }
}
