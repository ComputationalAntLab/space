using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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


        //find size of square to spawn ants into 
        float sqrt = Mathf.Ceil(Mathf.Sqrt(50));
        int count = 0;
        int scoutCount = 0;
        doors = GameObject.FindGameObjectsWithTag("Door");
        nests = new List<Transform>();
        Transform ants = MakeObject("Ants", null).transform;
        MakeObject("Pheromones", null);
        nests.Add(transform);

        GameObject[] newNests = GameObject.FindGameObjectsWithTag("NewNest");
        GameObject batchGO = GameObject.Find("BatchRunner");

        if (batchGO != null)
        {
            BatchRunner batchObj = (BatchRunner)batchGO.GetComponent("BatchRunner");
            if (batchObj != null)
            {
                this.quorumThreshold = batchObj.quorumThreshold;
                this.startScout = (this.proportionActive * (float)this.colonySize) - 1 * this.quorumThreshold;
            }
        }


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

        Transform passive = MakeObject("P0", ants).transform;
        MakeObject("S", ants);
        MakeObject("A0", ants);
        MakeObject("R0", ants);
        MakeObject("RT0", ants);
        MakeObject("F", ants);

        //just spawns ants in square around wherever this is placed
        while (count < this.colonySize)
        {
            int column = 0;
            while ((column == 0 || count % sqrt != 0) && count < colonySize)
            {
                float row = Mathf.Floor((float)count / sqrt);
                Vector3 pos = transform.position;
                GameObject newAnt = (GameObject)Instantiate(this.antPrefab, new Vector3(pos.x + row, 1.08f, pos.z + column), Quaternion.identity);
                newAnt.name = "Ant";
                if ((float)count < (float)colonySize * this.proportionActive)
                {
                    AntManager newAM = (AntManager)newAnt.transform.GetComponent("AntManager");
                    newAM.state = AntManager.State.Inactive;
                    newAM.passive = false;
                    newAM.myNest = GameObject.Find("OldNest");
                    newAM.oldNest = GameObject.Find("OldNest");
                    Transform senses = newAnt.transform.FindChild("Senses");
                    ((SphereCollider)senses.GetComponent("SphereCollider")).enabled = true;
                    ((SphereCollider)senses.GetComponent("SphereCollider")).radius = ((AntSenses)senses.GetComponent("AntSenses")).range;
                    ((AntSenses)newAnt.transform.FindChild("Senses").GetComponent("AntSenses")).enabled = true;
                    newAnt.transform.parent = passive;
                    newAM.inNest = true;
                    newAM.quorumThreshold = this.quorumThreshold;
                    newAM.rg = this.rg;

                    if ((float)scoutCount < this.startScout)
                    {
                        newAM.nextAssesment = 1;
                        scoutCount++;
                    }
                    else
                    {
                        newAM.nextAssesment = 0;
                    }
                }
                else
                {
                    newAnt.transform.parent = passive;
                    AntManager newAM = (AntManager)newAnt.transform.GetComponent("AntManager");
                    newAM.myNest = GameObject.Find("OldNest");
                    newAM.oldNest = GameObject.Find("OldNest");
                    newAnt.GetComponent<Renderer>().material.color = Color.black;
                    newAM.inNest = true;
                    newAM.quorumThreshold = this.quorumThreshold;
                    newAM.rg = this.rg;
                }

                column++;
                count++;
            }
        }

        //if this is batch running then write output
        if (batchGO != null)
        {
            gameObject.AddComponent<Output>();
            ((Output)transform.GetComponent("Output")).SetUp();

            //greg edit			
            //			gameObject.AddComponent("GregOutput");
            //			((GregOutput) transform.GetComponent("GregOutput")).SetUp();
        }
    }

    GameObject MakeObject(string name, Transform parent)
    {
        GameObject g = new GameObject();
        g.name = name;
        g.transform.parent = parent;
        return g;
    }
}
