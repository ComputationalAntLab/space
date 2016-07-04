using Assets.Scripts.Ants;
using System.Collections.Generic;
using System.IO;

namespace Assets.Scripts.Output
{
    public class AntDebugResults : Results
    {
        private Dictionary<int, BehaviourState> _stateHistory = new Dictionary<int, BehaviourState>();

        public AntDebugResults(SimulationManager simulation, string basePath)
            : base(simulation, Path.Combine(basePath, "ants_debug"))
        {
            Write("Step,AntId,PerceivedTicks");
        }

        public override void Step(long step)
        {
            foreach (var ant in Simulation.Ants)
            {
                    Write(string.Format("{0},{1},{2}", step, ant.AntId, ant.PerceivedTicks));
            }
        }
    }
}
