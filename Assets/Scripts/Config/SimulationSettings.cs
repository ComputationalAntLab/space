using System;
using System.Xml.Serialization;
using UnityEngine;
using System.Collections.Generic;
using Assets.Common;

namespace Assets.Scripts.Config
{
    [Serializable]
    public class SimulationSettings
    {
        [SerializeField]
        public string ExperimentName { get; set; }

        [SerializeField]
        public RandomSeed RandomSeed { get; set; }

        [SerializeField]
        public ColonySize ColonySize { get; set; }

        [SerializeField]
        public QuorumThreshold QuorumThreshold { get; set; }

        [SerializeField]
        public ProportionActive ProportionActive { get; set; }

        [SerializeField]
        public StartingTickRate StartingTickRate { get; set; }

        [XmlIgnore]
        public List<SimulationPropertyBase> AllProperties { get { return _allProperties.Value; } }

        private Lazy<List<SimulationPropertyBase>> _allProperties;

        // Sections for colony
        // Sections for ant behaviour etc
        // Sections for map

        public SimulationSettings()
        {
            ExperimentName = "Experiment1";

            RandomSeed = new RandomSeed();
            ColonySize = new ColonySize();
            QuorumThreshold = new QuorumThreshold();
            ProportionActive = new ProportionActive();
            StartingTickRate = new StartingTickRate();

            _allProperties = new Lazy<List<SimulationPropertyBase>>(() =>
             {
                 return new List<SimulationPropertyBase>
                {
                    RandomSeed,
                    ColonySize,
                    QuorumThreshold,
                    ProportionActive,
                    StartingTickRate
               };
             });
        }
    }
}
