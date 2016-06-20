using Assets.Scripts.Config;
using Assets.Scripts.Output;
using Assets.Scripts.Ticking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Assets
{
    public class ResultsManager : IDisposable, ITickable
    {
        public SimulationManager Simulation { get; private set; }

        public bool ShouldBeRemoved { get { return false; } }

        private List<Results> results;

        public ResultsManager(SimulationManager simulation)
        {
            Simulation = simulation;
            SetupOutput();
        }

        private void SetupOutput()
        {
            var outDir = "Results";

            if (!Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);

            var experimentName = DateTime.Now.ToString("yyyyMMddHHmm");

            var experimentPath = Path.Combine(outDir, experimentName);

            if (!Directory.Exists(experimentPath))
                Directory.CreateDirectory(experimentPath);

            using(StreamWriter sw = new StreamWriter(Path.Combine(experimentPath, "settings.xml")))
            {
                XmlSerializer xml = new XmlSerializer(typeof(SimulationSettings));

                xml.Serialize(sw, Simulation.Settings);
            }

            results = new List<Results>
            {
                new NestResults(Simulation, experimentPath),
                new AntResults(Simulation, experimentPath)
                
            };
        }
        
        public void Dispose()
        {
            foreach (var res in results)
                res.Dispose();
        }

        public void Tick(float elapsedSimulationMS)
        {
            foreach (var res in results)
                res.Step(Simulation.TickManager.CurrentTick);
        }
    }
}
