using UnityEngine;
using Assets.Scripts.Config;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ConfigMenu : MonoBehaviour
{
    private SimulationSettings _settings = new SimulationSettings();

    void Start()
    {
        foreach (var v in _settings.AllProperties)
            CreateInput(v);

        var start = GameObject.Find("Start").GetComponent< Button>();

        start.onClick.AddListener(Start_Clicked);
    }

    private void Start_Clicked()
    {
        var level = GameObject.Find("Level").GetComponent<Dropdown>();

        SceneManager.LoadScene(level.options[level.value].text);
    }

    int num = 0;
    private void CreateInput(SimulationPropertyBase property)
    {
        var properties = GameObject.Find("Properties");
        var content = properties.transform.Find("Viewport").Find("Content");

        var inputControl = Resources.Load("InputPrefab") as GameObject;

        var a = GameObject.Instantiate(inputControl);

        a.transform.SetParent(content.transform);

        //a.transform.position = new Vector3(-240, 45 + -(0 + (num * 35)), 0);
        //a.transform.position = new Vector3(-240, 45 + (0 + (num * 35)), 0);
        a.GetComponent<RectTransform>().anchoredPosition = new Vector2(-230, 137 + -(num * 35));

        a.transform.Find("Label").GetComponent<Text>().text = property.Name;
        var input = a.GetComponentInChildren<InputField>();

        input.text = property.GetValue();
        input.onValueChanged.AddListener(new InputWrapper(input, property).OnChange);


        num++;
    }

    private class InputWrapper
    {
        public InputField Field { get; private set; }
        public SimulationPropertyBase Property { get; private set; }

        public InputWrapper(InputField field, SimulationPropertyBase property)
        {
            Field = field;
            Property = property;
        }

        public void OnChange(string newValue)
        {
            Field.text = Property.SetValue(newValue);
        }
    }

    void Update()
    {

    }
}
