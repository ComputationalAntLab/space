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
        public int QuorumThreshold { get; set; }

        [SerializeField]
        public float ProportionActive { get; set; }

        // Sections for colony
        // Sections for ant behaviour etc
        // Sections for map

        public SimulationSettings()
        {
            ExperimentName = "Experiment1";
            ColonySize = 200;
            QuorumThreshold = 5;
        }

        public bool Validate()
        {
            Validate("ColonySize", ColonySize, 0, null);
            Validate("QuorumThreshold", QuorumThreshold, 0, null);
            Validate("ProportionActive", ProportionActive, 0, 1);

            return true;
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
                    message = string.Format("Must be greater than \"{0}\" and less than \"{1}\"", min,max);

                throw new ArgumentOutOfRangeException(param, message);
            }
        }
    }
}
