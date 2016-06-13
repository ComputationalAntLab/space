namespace Assets.Scripts
{
    public interface ITickable
    {
        void Tick(float elapsedSimulationMS);

        bool ShouldBeRemoved { get; }
    }
}
