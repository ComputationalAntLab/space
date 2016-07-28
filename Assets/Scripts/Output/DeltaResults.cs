namespace Assets.Scripts.Output
{
    public abstract class DeltaResults : Results
    {
        public override bool IsDelta { get { return true; } }

        public DeltaResults(SimulationManager simulation, string fileNameWithoutExtension) :
            base(simulation, fileNameWithoutExtension)
        {
        }
    }
}
