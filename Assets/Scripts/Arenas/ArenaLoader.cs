using Assets.Scripts.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.Arenas
{
    public class ArenaLoader : MonoBehaviour
    {
        private SimulationSettings _settings;
        private bool _instantiated;

        public void Load(SimulationSettings settings)
        {
            _settings = settings;
            SceneManager.LoadScene("FromFile");
        }
        
        void Update()
        {
            if (!_instantiated)
            {
                _instantiated = true;
                InstantiateArena();
            }
        }

        private void InstantiateArena()
        {
        }
    }
}
