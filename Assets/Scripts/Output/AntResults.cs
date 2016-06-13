namespace Assets.Scripts.Output
{
    public class AntResults : Results
    {
        public AntResults(SimulationManager simulation, string experiment)
            :base(simulation, experiment + "_ants")
        {
            Write("Step,AntId,State,Position");
        }

        public override void Step(int step)
        {
            foreach (var ant in Simulation.Ants)
                Write(string.Format("{0},{1},{2},{3}", step, ant.name, ant.state, ant.transform.position));
        }
    }
}
