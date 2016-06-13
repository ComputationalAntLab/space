using UnityEngine;
using UnityEngine.UI;

public class NestCountControl : MonoBehaviour
{
    private Text txtAssessing;
    private Text txtNestId;
    private Text txtPassive;
    private Text txtRecruiting;
    private Text txtReversing;
    
    void Start ()
    {
        txtNestId = transform.FindChild("txtNestId").GetComponent<Text>();
        txtPassive = transform.FindChild("txtPassive").GetComponent<Text>();
        txtAssessing = transform.FindChild("txtAssessing").GetComponent<Text>();
        txtRecruiting = transform.FindChild("txtRecruiting").GetComponent<Text>();
        txtReversing = transform.FindChild("txtReversing").GetComponent<Text>();
    }

    public void SetData(int nestId, int passive, int assessing,int recruiting, int reversing)
    {
        txtNestId.text = nestId.ToString();
        txtPassive.text = passive.ToString();
        txtAssessing.text = assessing.ToString();
        txtRecruiting.text = recruiting.ToString();
        txtReversing.text = reversing.ToString();
    }
}
