using Assets.Common;
using System.Collections.Generic;
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

    private Lazy<List<NestCountControl>> _nestCountControls;

    void Start()
    {
        Simulation = GameObject.FindObjectOfType<SimulationManager>() as SimulationManager;

        _nestCountControls = new Lazy<List<NestCountControl>>(() =>
        {
            var nestCountControlPrefab = Resources.Load("NestNumbers") as GameObject;
            var nestCountControls = new List<NestCountControl>();

            for (int i = 0; i < Simulation.NestInfo.Count; i++)
            {
                var ctl = GameObject.Instantiate(nestCountControlPrefab);
                ctl.transform.SetParent(transform);

                ctl.GetComponent<RectTransform>().anchoredPosition = new Vector2(80 + (50 * i), -150);
                nestCountControls.Add(ctl.GetComponent<NestCountControl>());
            }

            return nestCountControls;
        });
        //txtNestId = GameObject.Find("txtNestId").GetComponent<Text>();
        //txtPassive = GameObject.Find("txtPassive").GetComponent<Text>();
        //txtAssessing = GameObject.Find("txtAssessing").GetComponent<Text>();
        //txtRecruiting = GameObject.Find("txtRecruiting").GetComponent<Text>();
        //txtReversing = GameObject.Find("txtReversing").GetComponent<Text>();
    }

    void Update()
    {
        for (int i = 0; i < _nestCountControls.Value.Count; i++)
        {
            _nestCountControls.Value[i].SetData(
                Simulation.NestInfo[i].NestId,
                Simulation.NestInfo[i].AntsInactive.transform.childCount,
                Simulation.NestInfo[i].AntsAssessing.transform.childCount,
                Simulation.NestInfo[i].AntsRecruiting.transform.childCount,
                Simulation.NestInfo[i].AntsReversing.transform.childCount
                );
        }
        //string id = "Nest:\t\t\t\t";
        //string passive = "Inactive:\t\t";
        //string assessing = "Assessing:\t\t";
        //string recruiting = "Recruiting:\t\t";
        //string reversing = "Reversing:\t\t";

        //foreach (var n in Simulation.NestInfo)
        //{
        //    id += n.NestId + "\t\t";
        //    passive += n.AntsInactive.transform.childCount + "\t\t";
        //    assessing += n.AntsAssessing.transform.childCount + "\t\t";
        //    recruiting += n.AntsRecruiting.transform.childCount + "\t\t";
        //    reversing += n.AntsReversing.transform.childCount + "\t\t";
        //}

        //txtNestId.text = id;
        //txtAssessing.text = assessing;
        //txtPassive.text = passive;
        //txtRecruiting.text = recruiting;
        //txtReversing.text = reversing;
    }
}
