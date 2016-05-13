using System;
using UnityEngine;

namespace Assets.Scripts
{
    [Serializable]
    public class SimulationSettings
    {
        [SerializeField]
        public string ExperimentName { get; set; }

        [SerializeField]
        public int RandomSeed { get; set; }

        [SerializeField]
        public int ColonySize { get; set; }

        [SerializeField]
        public int QuorumThreshold{ get; set; }

        // Sections for colony
        // Sections for ant behaviour etc
        // Sections for map

        public SimulationSettings()
        {
            ExperimentName = "Experiment1";
            ColonySize = 200;
            QuorumThreshold = 5;
        }
    }
}
