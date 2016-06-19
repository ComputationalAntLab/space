using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Extensions;

public class TimeController : MonoBehaviour
{
    private Text txtFPS;

    int _frameCounter = 0;
    float _timeCounter = 0.0f;
    float _lastFramerate = 0.0f;
    public float _refreshTime = 0.5f; //Refresh FPS every .5 seconds

    void Start()
    {
        txtFPS = this.TextByName("txtFPS");
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
            //This code will break if you set your m_refreshTime to 0, which makes no sense.
            _lastFramerate = (float)_frameCounter / _timeCounter;
            _frameCounter = 0;
            _timeCounter = 0.0f;
        }
    }
}
