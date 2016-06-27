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

            _allProperties = new Lazy<List<SimulationPropertyBase>>(() =>
             {
                 return new List<SimulationPropertyBase>
                {
                    RandomSeed,
                    ColonySize,
                    QuorumThreshold,
                    ProportionActive
               };
             });

            ColonySize.Value = 1;
        }

        private void Validate(string param, float value, float? min, float? max)
        {
            bool error = false;

            if (min.HasValue && value < min.Value)
                error = true;
            if (max.HasValue && value > max.Value)
                error = true;

            if (error)
            {

                string message = string.Empty;

                if (!min.HasValue && max.HasValue)
                    message = string.Format("Must be less than \"{0}\"", max);
                else if (min.HasValue && !max.HasValue)
                    message = string.Format("Must be greater than \"{0}\"", min);
                else if (min.HasValue && max.HasValue)
                    message = string.Format("Must be greater than \"{0}\" and less than \"{1}\"", min, max);

                throw new ArgumentOutOfRangeException(param, message);
            }
        }
    }
}
