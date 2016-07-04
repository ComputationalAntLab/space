namespace Assets.Scripts.Config
{
    public class RandomSeed : SimulationIntProperty
    {
        public override int? MaxValue { get { return null; } }

        public override int? MinValue { get { return null; } }

        public override string Name { get { return "Random Seed"; } }

        public override string Description { get { return "The seed number used to initialize the random number generator used by the simulation."; } }

        public RandomSeed()
        {
            //Value = new Random().Next();
            Value = 13;
        }
    }

    public class ColonySize : SimulationIntProperty
    {
        public override int? MaxValue { get { return null; } }

        public override int? MinValue { get { return 0; } }

        public override string Name { get { return "Colony Size"; } }

        public override string Description { get { return "The starting number of ants in the first nest."; } }

        public ColonySize()
        {
            Value = 200;
        }
    }

    public class QuorumThreshold : SimulationIntProperty
    {
        public override int? MaxValue { get { return null; } }

        public override int? MinValue { get { return 0; } }

        public override string Name { get { return "Quorum Threshold"; } }

        public override string Description { get { return "The perceived number of ants required to reach a quorum."; } }

        public QuorumThreshold()
        {
            Value = 5;
        }
    }

    public class StartingTickRate: SimulationIntProperty
    {
        public override int? MaxValue { get { return null; } }

        public override int? MinValue { get { return 0; } }

        public override string Name { get { return "Starting Tick Rate"; } }

        public override string Description { get { return "The starting simulated tick rate at which the simulation will run."; } }

        public StartingTickRate()
        {
            Value = 1;
        }
    }

    public class ProportionActive : SimulationFloatProperty
    {
        public override float? MaxValue { get { return 1; } }

        public override float? MinValue { get { return 0; } }

        public override string Name { get { return "Proportion Active"; } }

        public override string Description { get { return "The proportion of ants that are scouting and active."; } }

        public ProportionActive()
        {
            Value = 0.5f;
        }
    }
}