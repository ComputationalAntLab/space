﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Assets.Scripts.Output
{
    public class AntDeltaResults : Results
    {
        private Dictionary<int, AntManager.BehaviourState> _stateHistory = new Dictionary<int, AntManager.BehaviourState>();

        public AntDeltaResults(SimulationManager simulation, string basePath)
            : base(simulation, Path.Combine(basePath, "ants_delta"))
        {
            Write("Step,AntId,State,Position");
        }

        public override void Step(long step)
        {
            foreach (var ant in Simulation.Ants)
            {
                if (StateChanged(ant))
                    Write(string.Format("{0},{1},{2},{3}", step, ant.AntId, ant.state, ant.transform.position));
            }
        }

        private bool StateChanged(AntManager ant)
        {
            if (!_stateHistory.ContainsKey(ant.AntId))
            {
                _stateHistory.Add(ant.AntId, ant.state);
                return true;
            }

            if (_stateHistory[ant.AntId] != ant.state)
            {
                _stateHistory[ant.AntId] = ant.state;
                return true;
            }

            return false;
        }
    }
}
