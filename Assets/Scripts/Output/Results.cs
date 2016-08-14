using System;
using System.IO;

namespace Assets.Scripts.Output
{
    public abstract class Results : IDisposable
    {
        public SimulationManager Simulation { get; private set; }
        
        private StreamWriter _writer;

        public Results(SimulationManager simulation, string fileNameWithoutExtension)
        {
            Simulation = simulation;
            _writer = new StreamWriter(fileNameWithoutExtension + ".txt", false);
        }

        public abstract void Step(long step);

        protected void Write(string message)
        {
            _writer.Write(message);
        }

        protected void WriteLine(string message = "")
        {
            _writer.WriteLine(message);
        }

        public void Dispose()
        {
            BeforeDispose();

            if (_writer != null)
                _writer.Close();
        }

        protected virtual void BeforeDispose()
        {
        }

        public virtual void SimulationStarted()
        {

        }

        public virtual void SimulationStopped()
        {

        }
    }
}
