using System;
using UnityEngine;

namespace Assets.Scripts
{
    [Serializable]
    public class SimulationSettings
    {
        [SerializeField]
        public string ExperimentName { get; set; }

        // Sections for colony
        // Sections for ant behaviour etc
        // Sections for map

        public SimulationSettings()
        {
            ExperimentName = "Experiment1";
        }
    }
}
