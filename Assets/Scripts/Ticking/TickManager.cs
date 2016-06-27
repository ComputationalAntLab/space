using System.Collections.Generic;

namespace Assets.Scripts.Ticking
{
    public class TickManager
    {
        public long CurrentTick { get; set; }

        public float SimulatedMillisecondsPerTick { get; private set; }

        public int TicksPerFrame { get; set; }
        public float TotalElapsedSimulatedSeconds { get { return TotalElapsedSimulatedMilliseconds / 1000; } }
        public float TotalElapsedSimulatedMilliseconds { get; private set; }

        /// <summary>
        /// Set to true to perform a single tick, even if the speed is set to zero
        /// </summary>
        public bool TickOnce { get; set; }

        private List<ITickable> _entities = new List<ITickable>();

        public TickManager()
        {
            SetTicksPerSimulatedSecond(60);
            TicksPerFrame = 1;
        }

        private void SetTicksPerSimulatedSecond(int ticks)
        {
            SimulatedMillisecondsPerTick = 1000f / (float)ticks;
        }

        public void AddEntity(params ITickable[] entities)
        {
            if (entities != null && entities.Length > 0)
            {
                _entities.AddRange(entities);
            }
        }

        public void AddEntities(IEnumerable<ITickable> entities)
        {
            _entities.AddRange(entities);
        }

        public void Process()
        {
            if (TicksPerFrame <= 0 && TickOnce)
            {
                TickOnce = false;
                Tick();
            }
            else
            {
                for (int i = 0; i < TicksPerFrame; i++)
                    Tick();
            }
        }

        private void Tick()
        {
            float elapsed = SimulatedMillisecondsPerTick;

            for (int i = 0; i < _entities.Count; i++)
            {
                _entities[i].Tick(elapsed);
                if (_entities[i].ShouldBeRemoved)
                {
                    _entities.RemoveAt(i);
                    i--;
                }
            }
            TotalElapsedSimulatedMilliseconds += elapsed;
            CurrentTick++;
        }
    }
}
