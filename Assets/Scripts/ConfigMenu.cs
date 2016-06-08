using UnityEngine;
using Assets.Scripts.Config;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Assets.Scripts.Extensions;

public class ConfigMenu : MonoBehaviour
{
    public static SimulationSettings Settings { get; set; }

    void Start()
    {
        Settings = new SimulationSettings();
        //DontDestroyOnLoad(this);

        foreach (var v in Settings.AllProperties)
            CreateInput(v);

        var start = GameObject.Find("Start").GetComponent<Button>();

        start.GetComponentInChildren<Text>().text = "Run Simulation";

        start.onClick.AddListener(Start_Clicked);
    }

    private void Start_Clicked()
    {
        var level = GameObject.Find("LevelSelect").GetComponent<Dropdown>();

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
