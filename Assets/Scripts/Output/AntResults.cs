using System.IO;

namespace Assets.Scripts.Output
{
    public class AntResults : Results
    {
        public AntResults(SimulationManager simulation, string basePath)
            : base(simulation, Path.Combine(basePath, "ants"))
        {
            Write("Step,AntId,State,Position");
        }

        public override void Step(long step)
        {
            foreach (var ant in Simulation.Ants)
                Write(string.Format("{0},{1},{2},{3}", step, ant.AntId, ant.state, ant.transform.position));
        }
    }
}
