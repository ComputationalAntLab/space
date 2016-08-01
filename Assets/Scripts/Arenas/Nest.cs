using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts.Arenas
{
    [Serializable]
    public class Nest
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public int PositionX { get; set; }

        public int PositionY { get; set; }

        public float Quality { get; set; }
    }
}
