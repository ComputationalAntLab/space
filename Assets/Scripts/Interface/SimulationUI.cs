using UnityEngine;
using Assets.Scripts.Extensions;
using UnityEngine.UI;

public class SimulationUI : MonoBehaviour
{
    private Button btnToggle;
    private bool _uiVisible = true;

    void Start ()
    {
        btnToggle = this.ButtonByName("UIToggle");

        btnToggle.onClick.AddListener(btnToggle_Click);
        UpdateToggle();
	}

    private void btnToggle_Click()
    {
        _uiVisible = !_uiVisible;
        UpdateToggle();
    }

    private void UpdateToggle()
    {
        btnToggle.SetText(_uiVisible ? ">" : "<");

        foreach(var child in transform)
        {
            if (child == btnToggle.transform)
                continue;

            var ct = child as Transform;
            ct.gameObject.SetActive(_uiVisible);
        }
    }
}
