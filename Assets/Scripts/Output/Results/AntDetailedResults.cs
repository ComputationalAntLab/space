﻿using System.IO;

namespace Assets.Scripts.Output
{
    public class AntDetailedResults : FixedTickResults
    {
        public AntDetailedResults(SimulationManager simulation, string basePath)
            : base(simulation, Path.Combine(basePath, "ants_detail"))
        {
            Write("Step,AntId,State,Position");
        }

        protected override void OutputData(long step)
        {
            foreach (var ant in Simulation.Ants)
                Write(string.Format("{0},{1},{2},{3}", step, ant.AntId, ant.state, ant.transform.position));
        }
    }
}