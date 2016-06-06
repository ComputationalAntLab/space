using System;

namespace Assets.Scripts.Output
{
    public class AntResults : Results
    {
        public SimulationManager Simulation { get; set; }

        public AntResults(string experiment)
            :base(experiment + "_ants")
        {

        }

        public void LogStep()
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
