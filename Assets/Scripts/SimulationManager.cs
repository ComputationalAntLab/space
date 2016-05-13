using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Assets.Scripts;

public class SimulationManager : MonoBehaviour
{

    public List<Transform> nests;
    public GameObject[] doors;
    public float startScout;

    //Parameters
    public GameObject antPrefab;
    public int colonySize = 50;
    public float proportionActive = 0.5f;
    public int quorumThreshold;
    public RandomGenerator rg = new RandomGenerator();


    //This spawns all the ants and starts the simulation
    void Start()
    {
        // greg edit
        //
        //		colonySize = 2;
        //colonySize = 100;
        colonySize = 200;
        //

        doors = GameObject.FindGameObjectsWithTag(Naming.World.Doors);
        nests = new List<Transform>();

        MakeObject(Naming.ObjectGroups.Pheromones, null);
        nests.Add(transform);

        GameObject[] newNests = GameObject.FindGameObjectsWithTag(Naming.World.NewNests);
        GameObject arena = GameObject.FindGameObjectWithTag(Naming.World.Arena);

        BatchRunner batchObj = (BatchRunner)arena.GetComponent(Naming.Simulation.BatchRunner);
        if (batchObj != null)
        {
            this.quorumThreshold = batchObj.quorumThreshold;

        }
        else
        {
            quorumThreshold = 8;
        }

        this.startScout = (this.proportionActive * (float)this.colonySize) - 1 * this.quorumThreshold;

        Transform ants = MakeObject(Naming.ObjectGroups.Ants, null).transform;

        //set up various classes of ants
        for (int i = 0; i < newNests.Length; i++)
        {
            Transform t = newNests[i].transform;
            MakeObject("A" + nests.Count, ants);
            MakeObject("R" + nests.Count, ants);
            MakeObject("P" + nests.Count, ants);
            MakeObject("RT" + nests.Count, ants);
            this.nests.Add(t.transform);
        }

        MakeObject("S", ants);
        MakeObject("A0", ants);
        MakeObject("R0", ants);
        MakeObject("RT0", ants);
        MakeObject("F", ants);

        SpawnColony(ants);

        //if this is batch running then write output
        if (batchObj != null)
        {
            gameObject.AddComponent<Output>();
            ((Output)transform.GetComponent(Naming.Simulation.Output)).SetUp();

            //greg edit			
            //			gameObject.AddComponent("GregOutput");
            //			((GregOutput) transform.GetComponent("GregOutput")).SetUp();
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
        while (spawnedAnts < this.colonySize)
        {
            int column = 0;
            while ((column == 0 || spawnedAnts % sqrt != 0) && spawnedAnts < colonySize)
            {
                float row = Mathf.Floor((float)spawnedAnts / sqrt);
                Vector3 pos = transform.position;
                GameObject newAnt = (GameObject)Instantiate(this.antPrefab, new Vector3(pos.x + row, 1.08f, pos.z + column), Quaternion.identity);
                newAnt.name = CreateAntId(colonySize, spawnedAnts);
                if ((float)spawnedAnts < (float)colonySize * this.proportionActive)
                {
                    AntManager newAM = (AntManager)newAnt.transform.GetComponent(Naming.Ants.Behaviour);
                    newAM.state = AntManager.State.Inactive;
                    newAM.passive = false;
                    newAM.myNest = GameObject.Find(Naming.World.InitialNest);
                    newAM.oldNest = GameObject.Find(Naming.World.InitialNest);
                    Transform senses = newAnt.transform.FindChild(Naming.Ants.SensesArea);
                    ((SphereCollider)senses.GetComponent("SphereCollider")).enabled = true;
                    ((SphereCollider)senses.GetComponent("SphereCollider")).radius = ((AntSenses)senses.GetComponent(Naming.Ants.SensesScript)).range;
                    ((AntSenses)senses.GetComponent(Naming.Ants.SensesScript)).enabled = true;
                    newAnt.transform.parent = passive;
                    newAM.inNest = true;
                    newAM.quorumThreshold = this.quorumThreshold;
                    newAM.rg = this.rg;

                    if ((float)spawnedAntScounts < this.startScout)
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
                    newAnt.transform.parent = passive;
                    AntManager newAM = (AntManager)newAnt.transform.GetComponent(Naming.Ants.Behaviour);
                    newAM.myNest = GameObject.Find(Naming.World.InitialNest);
                    newAM.oldNest = GameObject.Find(Naming.World.InitialNest);
                    newAnt.GetComponent<Renderer>().material.color = Color.black;
                    newAM.inNest = true;
                    newAM.quorumThreshold = this.quorumThreshold;
                    newAM.rg = this.rg;
                }

                column++;
                spawnedAnts++;
            }
        }
    }

    private string CreateAntId(int colonySize, int antNumber)
    {
        return Naming.Ants.CarryAnt;
        //return string.Format("{0}{1}", Naming.Entities.AntPrefix, antNumber);
    }

    GameObject MakeObject(string name, Transform parent)
    {
        GameObject g = new GameObject();
        g.name = name;
        g.transform.parent = parent;
        return g;
    }
}
