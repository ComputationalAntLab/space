using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Assets.Scripts;

public class Output : MonoBehaviour
{

    Transform s;
    List<Transform> a, r, p;
    public UnityEngine.GameObject[] ants;
    StreamWriter sw;
    int c;
    float timeStep = 1f;
    public SimData History;

    public void SetUp()
    {
        //get all ant state container objects
        a = new List<Transform>();
        r = new List<Transform>();
        p = new List<Transform>();
        s = GameObject.Find(Naming.Ants.BehavourState.Scouting).transform;
        p.Add(GameObject.Find(Naming.Ants.BehavourState.Inactive + "0").transform);
        for (int i = 1; i <= GameObject.Find(Naming.World.NewNests).transform.childCount; i++)
        {
            p.Add(GameObject.Find(Naming.Ants.BehavourState.Inactive + i).transform);
            a.Add(GameObject.Find(Naming.Ants.BehavourState.Assessing + i).transform);
            r.Add(GameObject.Find(Naming.Ants.BehavourState.Recruiting + i).transform);
        }

        GameObject batchGO = GameObject.Find(Naming.Simulation.BatchRunner);
        BatchRunner batch = (BatchRunner)batchGO.transform.GetComponent(Naming.Simulation.BatchRunner);
        string outputFile = batch.GetNextOutputFile();
        int quorumThresh = 0;// batch.quorumThreshold; // TODO: set quorum threshhold to file

        try
        {
            sw = new StreamWriter(outputFile);
        }
        catch
        {
            print("Couldn't find output file");
            return;
        }
        sw.Write("Space Data");
        sw.WriteLine();
        sw.Write("QuorumThresh = " + quorumThresh);
        sw.WriteLine();
        sw.Write("Colony size = 200");
        sw.WriteLine();

        //write column titles
        sw.Write("T, ");
        sw.Write("S, ");
        for (int i = 0; i < p.Count; i++)
        {
            sw.Write("P" + i + ", ");
        }
        for (int i = 1; i <= a.Count; i++)
        {
            sw.Write("A" + i + ", "); ;
        }
        for (int i = 1; i <= r.Count; i++)
        {
            if (i == r.Count)
                sw.Write("R" + i);
            else
                sw.Write("R" + i + ", ");
        }
        sw.WriteLine();
        c = 0;

        //make writestatestofile be called every timeStep
        InvokeRepeating("WriteStatesToFile", 0f, timeStep);
    }

    void WriteStatesToFile()
    {
        //check if setup
        if (s == null || c < 0) return;

        sw.Write(Mathf.Round(c * timeStep) + ", ");
        sw.Write(s.childCount + ", ");
        for (int i = 0; i < p.Count; i++)
        {
            sw.Write(p[i].childCount + ", ");
        }
        for (int i = 0; i < a.Count; i++)
        {
            sw.Write(a[i].childCount + ", ");
        }
        for (int i = 0; i < r.Count; i++)
        {
            if (i == r.Count - 1)
                sw.Write(r[i].childCount);
            else
                sw.Write(r[i].childCount + ", ");
        }
        sw.WriteLine();
        c++;

        //if there are no passive ants left in the original nest then restart the simulation
        if (p[0].childCount == 0)
        {
            c = -1;
            WriteFinalState();
            sw.Close();
            ((BatchRunner)GameObject.Find(Naming.Simulation.BatchRunner).GetComponent(Naming.Simulation.BatchRunner)).StartExperiment();
        }
    }

    void WriteFinalState()
    {
        ants = GameObject.FindGameObjectsWithTag("Ant");
        sw.Write("State History Begins");
        sw.WriteLine();

        sw.Write("Left, FT, FC, NT, NC, NS, FR, NR,");
        for (int i = 1; i < p.Count; i++)
        {
            sw.Write("Found " + i + ", ");
        }
        for (int i = 1; i < p.Count; i++)
        {
            sw.Write("Recruited to " + i + ", ");
        }
        for (int i = 1; i < p.Count; i++)
        {
            sw.Write("Assessed " + i + ", ");
        }
        for (int i = 1; i < p.Count; i++)
        {
            sw.Write("Accepted" + i + ", ");
        }
        sw.Write("State History");
        sw.WriteLine();

        foreach (UnityEngine.GameObject ant in ants)
        {
            SimData Data = (SimData)ant.GetComponent(Naming.Simulation.AntData);
            sw.Write(Data.LeftOld + ", ");
            sw.Write(Data.firstTandem + ", ");
            sw.Write(Data.firstCarry + ", ");
            sw.Write(Data.numTandem + ", ");
            sw.Write(Data.numCarry + ", ");
            sw.Write(Data.numSwitch + ", ");
            sw.Write(Data.firstRev + ", ");
            sw.Write(Data.numRev + ", ");

            StringBuilder builder = new StringBuilder();
            foreach (int nestTime in Data.NestDiscoveryTime)
            {
                // Append each int to the StringBuilder overload.
                builder.Append(nestTime).Append(" ");
            }
            string result = builder.ToString();
            sw.Write(result + ", ");

            builder = new StringBuilder();
            foreach (int nestTime in Data.NestRecruitTime)
            {
                // Append each int to the StringBuilder overload.
                builder.Append(nestTime).Append(" ");
            }
            result = builder.ToString();
            sw.Write(result + ", ");

            builder = new StringBuilder();
            foreach (int assessments in Data.numAssessments)
            {
                // Append each int to the StringBuilder overload.
                builder.Append(assessments).Append(" ");
            }
            result = builder.ToString();
            sw.Write(result + ", ");

            builder = new StringBuilder();
            foreach (int acceptances in Data.numAcceptance)
            {
                // Append each int to the StringBuilder overload.
                builder.Append(acceptances).Append(" ");
            }
            result = builder.ToString();
            sw.Write(result + ", ");

            builder = new StringBuilder();
            foreach (AntManager.State state in Data.StateHistory)
            {
                // Append each int to the StringBuilder overload.
                builder.Append(state).Append(" ");
            }
            result = builder.ToString();
            sw.Write(result);

            //sw.Write((string)Data.StateHistory+ ", ");
            sw.WriteLine();
        }
        //Debug.Log(ants.Count);
        return;
    }
}
