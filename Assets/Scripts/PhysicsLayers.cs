using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts
{
    public static class PhysicsLayers
    {
        public static int Ants { get; private set; }
        public static int Walls { get; private set; }
        public static int AntsAndWalls { get; private set; }

        static PhysicsLayers()
        {
            // Layer masks work with bit operations

            // Unity has dibs on the first 8 layers
            int antLayer = 8;
            int wallLayer = 9;

            Ants = 1 << antLayer;
            Walls = 1 << wallLayer;
            AntsAndWalls = Ants | Walls;
        }
    }
}
