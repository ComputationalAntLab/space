using Assets.Scripts.Ants;
using System;
using System.Collections.Generic;
using System.IO;

namespace Assets.Scripts.Output
{
    public class ExecutionResults : DeltaResults
    {
        private DateTime _start = DateTime.Now;

        public ExecutionResults(SimulationManager simulation, string basePath)
            : base(simulation, Path.Combine(basePath, "execution"))
        {
            Write("Start: " + _start.ToString("dd/MM/yyyy HH:mm:ss"));
        }

        public override void Step(long step)
        {
        }

        protected override void BeforeDispose()
        {
            base.BeforeDispose();
            var end = DateTime.Now;
            var duration = end - _start;
            var simDuration = Simulation.TickManager.TotalElapsedSimulatedTime;

            Write("End: " + end.ToString("dd/MM/yyyy HH:mm:ss"));
            Write("Execution Duration: " + string.Format("{0}:{1}:{2}", duration.TotalHours, duration.Minutes, duration.Seconds));
            Write("Simulation Duration: " + string.Format("{0}:{1}:{2}", simDuration.TotalHours, simDuration.Minutes, simDuration.Seconds));
        }
    }
}
