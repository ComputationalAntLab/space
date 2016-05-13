namespace Assets.Scripts
{
    public static class Naming
    {
        public static class World
        {
            public const string Doors = "Door";
            public const string NewNests = "NewNest";
            public const string InitialNest = "OldNest";
            public const string Arena = "Arena";
            public const string Nest = "NestManager";
        }

        public static class Simulation
        {
            public const string Manager = "SimulationManager";
            public const string BatchRunner = "BatchRunner";
            public const string Output = "Output";
            public const string AntData = "SimData";
        }

        public static class Ants
        {
            public const string Controller = "AntManager";
            public const string Movement= "AntMovement";
            public const string SensesArea = "Senses";
            public const string SensesScript = "AntSenses";

            public const string CarryPosition = "CarryPosition";
            public const string Tag = "Ant";

            public const string Pheromone = "Pheromone";
        }

        public static class ObjectGroups
        {
            public const string Pheromones = "Pheromones";
            public const string Ants = "Ants";
        }

        public static class Entities
        {
            // Can't change the ant prefix because each ant needs to be called "Ant"
            public const string AntPrefix = "";
        }
    }
}
