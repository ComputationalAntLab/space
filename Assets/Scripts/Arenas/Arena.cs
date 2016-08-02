using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts.Arenas
{
    [Serializable]
    public class Arena
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public int AveragedSize { get { return (Width + Height) / 2; } }

        public Nest StartingNest { get; set; }

        public List<Nest> NewNests { get; set; }

        public Arena()
        {
            NewNests = new List<Nest>();
        }
    }
}
