using System;

namespace Assets.Scripts.Output
{
    public class AntResults : Results
    {
        public AntResults(SimulationManager simulation, string experiment)
            :base(simulation, experiment + "_ants")
        {

        }

        public override void Step(int step)
        {
            //from ant in Simulation.Ants
            //group ant.state
            //select WriteAntGroup(a)
        }

        private void WriteAnt(AntManager ant)
        {
            throw new NotImplementedException();
        }
    }
}
