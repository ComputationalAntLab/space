using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts
{
    public class TickManager
    {
        public long CurrentTick { get; set; }

        public void Update()
        {
            Tick();
        }

        private void Tick()
        {
            CurrentTick++;
        }
    }
}
