using System;

namespace Assets.Scripts.Output
{
    public class NestResults : Results
    {
        public NestResults(SimulationManager simulation, string experiment)
            :base(simulation,experiment + "_colony")
        {
            Write("NestId,Inactive,Assessing,Recruiting,Reversing");
        }

        public override void Step(int step)
        {
            foreach(var nest in Simulation.NestInfo)
            {
                Write(string.Format("{0},{1},{2},{3},{4}", nest.NestId,
                    nest.AntsInactive.transform.childCount,
                    nest.AntsAssessing.transform.childCount,
                    nest.AntsRecruiting.transform.childCount,
                    nest.AntsReversing.transform.childCount));
            }
        }
    }
}
