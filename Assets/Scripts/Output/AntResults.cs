using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        }
    }
}
