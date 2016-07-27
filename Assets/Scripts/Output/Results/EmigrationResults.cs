using System.IO;
using System.Linq;

namespace Assets.Scripts.Output
{
    public class EmigrationResults : FixedTickResults
    {
        public EmigrationResults(SimulationManager simulation, string basePath)
            : base(simulation, Path.Combine(basePath, "emigration"))
        {
            Write("Step,Property,Value");
        }

        protected override void OutputData(long step)
        {
            Write(string.Format("{0},{1},{2}", step, "PassivesInOldNest", Simulation.EmigrationInformation.Data.PassivesInOldNest));
            foreach(var o in Simulation.EmigrationInformation.Data.PassivesInNewNests)
                Write(string.Format("{0},{1},{2}", step, "PassivesInNewNest_" + o.Key, o.Value));
            Write(string.Format("{0},{1},{2}", step, "Completion", Simulation.EmigrationInformation.Data.EmigrationCompletion));
            Write(string.Format("{0},{1},{2}", step, "RelativeAccuracy", Simulation.EmigrationInformation.Data.EmigrationRelativeAccuracy));
            Write(string.Format("{0},{1},{2}", step, "AbsoluteAccuracy", Simulation.EmigrationInformation.Data.EmigrationAbsoluteAccuracy));
        }
    }
}
