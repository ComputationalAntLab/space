using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Extensions;
using Assets.Scripts.Config;
using System.Linq;
using Assets;
using Assets.Scripts.Ticking;
using Assets.Scripts.Nests;

public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance { get; private set; }

    public List<Transform> nests = new List<Transform>();
    public List<NestInfo> NestInfo { get; private set; }
    public GameObject[] doors;

    public TickManager TickManager { get; private set; }
    public ResultsManager ResultsManager { get; private set; }

    public List<AntManager> Ants { get; private set; }

    public SimulationSettings Settings { get; private set; }

    //Parameters
    private GameObject initialNest;

    public float InitialScouts { get { return (Settings.ProportionActive.Value * Settings.ColonySize.Value) - 1 * Settings.QuorumThreshold.Value; } }


    //This spawns all the ants and starts the simulation
    void Start()
    {
        Instance = this;

        Ants = new List<AntManager>();
        NestInfo = new List<NestInfo>();

        // TODO: load from file
        Settings = ConfigMenu.Settings;
        if (Settings == null)
            Settings = new SimulationSettings();

        RandomGenerator.Init(Settings.RandomSeed.Value);

        doors = GameObject.FindGameObjectsWithTag(Naming.World.Doors);

        initialNest = GameObject.Find(Naming.World.InitialNest);
        initialNest.Nest().simulation = this;
        nests.Add(initialNest.transform);

        GameObject[] newNests = GameObject.FindGameObjectsWithTag(Naming.World.NewNests);
        GameObject arena = GameObject.FindGameObjectWithTag(Naming.World.Arena);

        MakeObject(Naming.ObjectGroups.Pheromones, null);
        Transform antHolder = MakeObject(Naming.ObjectGroups.Ants, null).transform;

        // For some reason scouting isnt suffixed with nest number - perhaps because scouting doesnt need a nest
        MakeObject(Naming.Ants.BehavourState.Scouting, antHolder);
        MakeObject("F", antHolder);

        SpawnColony(antHolder);

        //set up various classes of ants
        for (int i = 0; i < newNests.Length; i++)
        {
            Transform t = newNests[i].transform;

            this.nests.Add(t.transform);
            newNests[i].Nest().simulation = this;

            int id = i + 1;

            NestInfo.Add(new NestInfo(id, false,
                MakeObject(Naming.Ants.BehavourState.Assessing + id, antHolder),
                MakeObject(Naming.Ants.BehavourState.Recruiting + id, antHolder),
                MakeObject(Naming.Ants.BehavourState.Inactive + id, antHolder),
                MakeObject(Naming.Ants.BehavourState.Reversing + id, antHolder)
                ));
        }

        BatchRunner batchObj = null;// (BatchRunner)arena.GetComponent(Naming.Simulation.BatchRunner);
        //if this is batch running then write output
        if (batchObj != null)
        {
            gameObject.AddComponent<Output>();
            ((Output)transform.GetComponent(Naming.Simulation.Output)).SetUp();
        }

        ResultsManager = new ResultsManager(this);

        TickManager = new TickManager();
        TickManager.AddEntities(Ants.Cast<ITickable>());
        TickManager.AddEntity(ResultsManager);
    }

    private void SpawnColony(Transform ants)
    {
        var antPrefab = Resources.Load(Naming.Resources.AntPrefab) as GameObject;

        Transform passive = MakeObject("P0", ants).transform;

        NestInfo.Add(new NestInfo(0, true,
               MakeObject(Naming.Ants.BehavourState.Assessing + "0", ants),
               MakeObject(Naming.Ants.BehavourState.Recruiting + "0", ants),
                passive.gameObject,
               MakeObject(Naming.Ants.BehavourState.Reversing + "0", ants)
           ));

        // Local variables for ant setup
        //find size of square to spawn ants into 
        float sqrt = Mathf.Ceil(Mathf.Sqrt(50));
        int spawnedAnts = 0;
        int spawnedAntScounts = 0;

        //just spawns ants in square around wherever this is placed
        while (spawnedAnts < Settings.ColonySize.Value)
        {
            int column = 0;
            while ((column == 0 || spawnedAnts % sqrt != 0) && spawnedAnts < Settings.ColonySize.Value)
            {
                float row = Mathf.Floor(spawnedAnts / sqrt);
                Vector3 pos = initialNest.transform.position;

                GameObject newAnt = (GameObject)Instantiate(antPrefab, new Vector3(pos.x + row, 0, pos.z + column), Quaternion.identity);
                newAnt.name = CreateAntId(Settings.ColonySize.Value, spawnedAnts);
                newAnt.AntMovement().simManager = this;

                AntManager newAM = newAnt.AntManager();

                Ants.Add(newAM);

                newAM.AntId = spawnedAnts;
                newAM.myNest = initialNest;
                newAM.myNest = initialNest;
                newAM.simulation = this;
                newAM.inNest = true;
                newAM.quorumThreshold = Settings.QuorumThreshold.Value;
                newAnt.transform.parent = passive;

                if (spawnedAnts < Settings.ColonySize.Value * Settings.ProportionActive.Value)
                {
                    newAM.state = AntManager.BehaviourState.Inactive;
                    newAM.passive = false;

                    Transform senses = newAnt.transform.FindChild(Naming.Ants.SensesArea);
                    ((SphereCollider)senses.GetComponent("SphereCollider")).enabled = true;
                    ((SphereCollider)senses.GetComponent("SphereCollider")).radius = ((AntSenses)senses.GetComponent(Naming.Ants.SensesScript)).range;
                    ((AntSenses)senses.GetComponent(Naming.Ants.SensesScript)).enabled = true;

                    if (spawnedAntScounts < InitialScouts)
                    {
                        newAM.nextAssesment = 1;
                        spawnedAntScounts++;
                    }
                    else
                    {
                        newAM.nextAssesment = 0;
                    }
                }
                else
                {
                    // Passive ant
                    newAM.passive = true;
                    newAnt.GetComponent<Renderer>().material.color = Color.black;
                }

                column++;
                spawnedAnts++;
            }
        }
    }

    private string CreateAntId(int colonySize, int antNumber)
    {
        return Naming.Ants.Tag;
        //return string.Format("{0}{1}", Naming.Entities.AntPrefix, antNumber);
    }

    void Update()
    {
        TickManager.Process();
    }

    private GameObject MakeObject(string name, Transform parent)
    {
        GameObject g = new GameObject();
        g.name = name;
        g.transform.parent = parent;
        return g;
    }

    //returns the ID of the nest that is passed in
    public int GetNestID(GameObject nest)
    {
        return nests.IndexOf(nest.transform);
    }


    void OnDestroy()
    {
        ResultsManager.Dispose();
    }
}
