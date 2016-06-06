using UnityEngine;
using UnityEngine.UI;

public class NestCount : MonoBehaviour
{
    private Text txtAssessing;
    private Text txtNestId;
    private Text txtPassive;
    private Text txtRecruiting;
    private Text txtReversing;

    public SimulationManager Simulation { get; private set; }

    void Start()
    {
        Simulation = GameObject.FindObjectOfType<SimulationManager>() as SimulationManager;

        txtNestId = GameObject.Find("txtNestId").GetComponent<Text>();
        txtPassive = GameObject.Find("txtPassive").GetComponent<Text>();
        txtAssessing = GameObject.Find("txtAssessing").GetComponent<Text>();
        txtRecruiting = GameObject.Find("txtRecruiting").GetComponent<Text>();
        txtReversing = GameObject.Find("txtReversing").GetComponent<Text>();
    }

    void Update()
    {
        string id = "Nest:\t\t\t";
        string passive = "Inactive:\t\t";
        string assessing = "Assessing:\t\t";
        string recruiting = "Recruiting:\t\t";
        string reversing = "Reversing:\t\t";

        foreach (var n in Simulation.NestInfo)
        {
            id += n.NestId + "\t";
            passive += n.AntsInactive.transform.childCount + "\t";
            assessing += n.AntsAssessing.transform.childCount + "\t";
            recruiting += n.AntsRecruiting.transform.childCount + "\t";
            reversing += n.AntsReversing.transform.childCount + "\t";
        }

        txtNestId.text = id;
        txtAssessing.text = assessing;
        txtPassive.text = passive;
        txtRecruiting.text = recruiting;
        txtReversing.text = reversing;
    }
}
