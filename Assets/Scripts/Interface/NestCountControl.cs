using Assets.Scripts.Extensions;
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
        txtNestId = this.TextByName("txtNestId");
        txtPassive = this.TextByName("txtPassive");
        txtAssessing = this.TextByName("txtAssessing");
        txtRecruiting = this.TextByName("txtRecruiting");
        txtReversing = this.TextByName("txtReversing");
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