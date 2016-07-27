using System;
using System.IO;

namespace Assets.Scripts.Output
{
    public abstract class Results : IDisposable
    {
        public SimulationManager Simulation { get; private set; }
        
        public abstract bool IsDelta { get; }

        private StreamWriter _writer;

        public Results(SimulationManager simulation, string fileNameWithoutExtension)
        {
            Simulation = simulation;
            _writer = new StreamWriter(fileNameWithoutExtension + ".txt", false);
        }

        public abstract void Step(long step);

        protected void Write(string message)
        {
            _writer.WriteLine(message);
        }

        public void Dispose()
        {
            if (_writer != null)
                _writer.Close();
        }
    }
}
