using Assets.Scripts.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.Arenas
{
    public class ArenaLoader : MonoBehaviour
    {
        private bool _instantiated;
        private SimulationSettings _settings;
        private Arena _arena;
        private Vector3 _worldSize;

        public void Load(SimulationSettings settings)
        {
            _settings = settings;

            using (var sr = new StreamReader(_settings.ArenaFilename))
            {
                var xml = new XmlSerializer(typeof(Arena));

                _arena = xml.Deserialize(sr) as Arena;
            }

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
            CreateTerrain();
            MoveCameras();
        }

        private void CreateTerrain()
        {
            var obj = new GameObject("TerrainObj");

            var data = new TerrainData();

            data.size = new Vector3(_arena.Width / 16, 10, _arena.Height / 16);
            data.heightmapResolution = 512;
            data.baseMapResolution = 1024;
            data.SetDetailResolution(1024, 16);

            var grass = Resources.Load("Grass (Hill)") as Texture2D;

            data.splatPrototypes = new SplatPrototype[]
            {
                new SplatPrototype
                {
                    texture = grass
                }
            };

            var collider = obj.AddComponent<TerrainCollider>();
            var terrain = obj.AddComponent<Terrain>();

            collider.terrainData = data;
            terrain.terrainData = data;

            _worldSize = data.size;
        }

        private void MoveCameras()
        {
            var freeCamera = GameObject.FindGameObjectWithTag("FreeCamera").GetComponent<Camera>();
            var mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

            MoveCamera(mainCamera);
            MoveCamera(freeCamera);
        }

        private void MoveCamera(Camera camera)
        {
            var offset = (_worldSize.x + _worldSize.z) / 10;

            camera.transform.position = new Vector3(0, (_worldSize.x + _worldSize.z) / 8, 0);
            camera.transform.LookAt(new Vector3(_worldSize.x / 4.5f, 0, _worldSize.z / 4.5f));

            try
            {
                var freeCamera = camera.GetComponent<FreeCamera>();
                freeCamera.ResetView();
            }
            catch (Exception ex)
            {

            }
        }
    }
}
