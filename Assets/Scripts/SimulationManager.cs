using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts;

public class SimulationManager : MonoBehaviour
{

    public List<Transform> nests = new List<Transform>();
    public GameObject[] doors;


    public SimulationSettings Settings;

    //Parameters
    public GameObject antPrefab;
    private GameObject initialNest;


    public float InitialScouts { get { return (Settings.ProportionActive * Settings.ColonySize) - 1 * Settings.QuorumThreshold; } }

    //This spawns all the ants and starts the simulation
    void Start()
    {
        // TODO: load from file

        if (Settings == null)
            Settings = new SimulationSettings();

        RandomGenerator.Init(Settings.RandomSeed);

        doors = GameObject.FindGameObjectsWithTag(Naming.World.Doors);

        initialNest = GameObject.Find(Naming.World.InitialNest);
        initialNest.Nest().simulation = this;
        nests.Add(initialNest.transform);

        GameObject[] newNests = GameObject.FindGameObjectsWithTag(Naming.World.NewNests);
        GameObject arena = GameObject.FindGameObjectWithTag(Naming.World.Arena);

        MakeObject(Naming.ObjectGroups.Pheromones, null);
        Transform antHolder = MakeObject(Naming.ObjectGroups.Ants, null).transform;

        //set up various classes of ants
        for (int i = 0; i < newNests.Length; i++)
        {
            Transform t = newNests[i].transform;
            MakeObject("A" + nests.Count, antHolder);
            MakeObject("R" + nests.Count, antHolder);
            MakeObject("P" + nests.Count, antHolder);
            MakeObject("RT" + nests.Count, antHolder);
            this.nests.Add(t.transform);
            newNests[i].Nest().simulation = this;
        }

        MakeObject("S", antHolder);
        MakeObject("A0", antHolder);
        MakeObject("R0", antHolder);
        MakeObject("RT0", antHolder);
        MakeObject("F", antHolder);

        SpawnColony(antHolder);


        BatchRunner batchObj = null;// (BatchRunner)arena.GetComponent(Naming.Simulation.BatchRunner);
        //if this is batch running then write output
        if (batchObj != null)
        {
            gameObject.AddComponent<Output>();
            ((Output)transform.GetComponent(Naming.Simulation.Output)).SetUp();
        }
    }

    private void SpawnColony(Transform ants)
    {
        Transform passive = MakeObject("P0", ants).transform;

        // Local variables for ant setup
        //find size of square to spawn ants into 
        float sqrt = Mathf.Ceil(Mathf.Sqrt(50));
        int spawnedAnts = 0;
        int spawnedAntScounts = 0;

        //just spawns ants in square around wherever this is placed
        while (spawnedAnts < Settings.ColonySize)
        {
            int column = 0;
            while ((column == 0 || spawnedAnts % sqrt != 0) && spawnedAnts < Settings.ColonySize)
            {
                float row = Mathf.Floor(spawnedAnts / sqrt);
                Vector3 pos = initialNest.transform.position;

                GameObject newAnt = (GameObject)Instantiate(antPrefab, new Vector3(pos.x + row, 1.08f, pos.z + column), Quaternion.identity);
                newAnt.name = CreateAntId(Settings.ColonySize, spawnedAnts);
                newAnt.AntMovement().simManager = this;

                AntManager newAM = newAnt.AntManager();

                newAM.myNest = initialNest;
                newAM.myNest = initialNest;
                newAM.simulation = this;
                newAM.inNest = true;
                newAM.quorumThreshold = Settings.QuorumThreshold;
                newAnt.transform.parent = passive;

                if (spawnedAnts < Settings.ColonySize * Settings.ProportionActive)
                {
                    newAM.state = AntManager.State.Inactive;
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
}
