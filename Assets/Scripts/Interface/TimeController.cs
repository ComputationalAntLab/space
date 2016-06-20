using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Extensions;

public class TimeController : MonoBehaviour
{
    private Text txtFPS, txtSpeed;

    private Button btnUp, btnDown;

    int _frameCounter = 0;
    float _timeCounter = 0.0f;
    float _lastFramerate = 0.0f;
    public float _refreshTime = 0.5f; //Refresh FPS every .5 seconds

    private int _currentSpeed = 1;

    void Start()
    {
        txtFPS = this.TextByName("txtFPS");
        txtSpeed= this.TextByName("txtSpeed");

        btnUp = this.ButtonByName("btnUp");
        btnDown = this.ButtonByName("btnDown");

        btnUp.GetComponentInChildren<Text>().text = "+";
        btnDown.GetComponentInChildren<Text>().text = "-";

        btnUp.onClick.AddListener(btnUp_Click);
        btnDown.onClick.AddListener(btnDown_Click);
    }

    private void btnDown_Click()
    {
        ModifySpeed(-1);
    }

    private void btnUp_Click()
    {
        ModifySpeed(1);
    }

    private void ModifySpeed(int by)
    {
        int newSpeed = _currentSpeed += by;

        if (newSpeed < 0)
            newSpeed = 0;

        _currentSpeed = newSpeed;

        txtSpeed.text = _currentSpeed + "x";

        SimulationManager.Instance.TickManager.TicksPerFrame = _currentSpeed;
    }

    void Update()
    {
        UpdateFPS();
    }

    private void UpdateFPS()
    {
        if (_timeCounter < _refreshTime)
        {
            _timeCounter += Time.deltaTime;
            _frameCounter++;
        }
        else
        {
            txtFPS.text = string.Format("FPS: {0}", _lastFramerate);

            _lastFramerate = (float)_frameCounter / _timeCounter;
            _frameCounter = 0;
            _timeCounter = 0.0f;
        }
    }
}
