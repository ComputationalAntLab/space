using System.IO;

namespace Assets.Scripts.Output
{
    public class ColonyResults : FixedTickResults
    {
        public ColonyResults(SimulationManager simulation, string basePath)
            : base(simulation, Path.Combine(basePath, "colony"))
        {
            Write("Step,NestId,Inactive,Assessing,Recruiting,Reversing");
        }

        protected override void OutputData(long step)
        {
            foreach(var nest in Simulation.NestInfo)
            {
                Write(string.Format("{0},{1},{2},{3},{4},{5}", step, nest.NestId,
                    nest.AntsInactive.transform.childCount,
                    nest.AntsAssessing.transform.childCount,
                    nest.AntsRecruiting.transform.childCount,
                    nest.AntsReversing.transform.childCount));
            }
        }
    }
}
