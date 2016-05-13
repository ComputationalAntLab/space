using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        }

        public static class Components
        {
            public const string BatchRunner = "BatchRunner";
            public const string Output = "Output";

            public static class Ants
            {
                public const string Behaviour = "AntManager";
                public const string SensesArea = "Senses";
                public const string SensesScript = "AntSenses";
            }
        }

        public static class ObjectGroups
        {
            public const string Pheromones = "Pheromones";
            public const string Ants = "Ants";
        }

        public static class Entities
        {
            public const string AntPrefix = "Ant_";
        }
    }
}
