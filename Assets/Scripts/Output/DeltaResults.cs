using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
