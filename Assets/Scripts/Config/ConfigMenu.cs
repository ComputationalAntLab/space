using UnityEngine;
using Assets.Scripts.Config;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.IO;
using System.Xml.Serialization;

public class ConfigMenu : MonoBehaviour
{
    public static SimulationSettings Settings { get; set; }

    void Start()
    {
        //DontDestroyOnLoad(this);
        Load(new SimulationSettings());

        var start = GameObject.Find("Start").GetComponent<Button>();

        start.GetComponentInChildren<Text>().text = "Run Simulation";

        start.onClick.AddListener(Start_Clicked);

        var save = GameObject.Find("Save").GetComponent<Button>();
        save.onClick.AddListener(Save_Clicked);
        save.GetComponentInChildren<Text>().text = "Save";
        var load = GameObject.Find("Load").GetComponent<Button>();
        load.onClick.AddListener(Load_Clicked);
        load.GetComponentInChildren<Text>().text = "Load";
    }

    private void Load(SimulationSettings settings)
    {
        Settings = settings;
        num = 0;
        GetPropertiesContentArea().DetachChildren();
        foreach (var v in Settings.AllProperties)
            CreateInput(v);
    }

    private void Load_Clicked()
    {
        var file = EditorUtility.OpenFilePanel("Load File", string.Empty, "xml");

        using (var sr = new StreamReader(file))
        {
            var xml = new XmlSerializer(typeof(SimulationSettings));

            var settings = xml.Deserialize(sr) as SimulationSettings;

            if (settings != null)
            {
                Load(settings);
            }
        }
    }

    private void Save_Clicked()
    {
        var file = EditorUtility.SaveFilePanel("Save File", string.Empty, "space.xml", "xml");

        using (var sr = new StreamWriter(file))
        {
            var xml = new XmlSerializer(typeof(SimulationSettings));

            xml.Serialize(sr, Settings);
        }
    }

    private void Start_Clicked()
    {
        var level = GameObject.Find("LevelSelect").GetComponent<Dropdown>();

        SceneManager.LoadScene(level.options[level.value].text);
    }

    int num = 0;
    private void CreateInput(SimulationPropertyBase property)
    {
        Transform content = GetPropertiesContentArea();

        var inputControl = (property is SimulationBoolProperty ? Resources.Load("InputBool") : Resources.Load("InputText")) as GameObject;

        var a = GameObject.Instantiate(inputControl);

        a.transform.SetParent(content.transform);

        //a.transform.position = new Vector3(-240, 45 + -(0 + (num * 35)), 0);
        //a.transform.position = new Vector3(-240, 45 + (0 + (num * 35)), 0);
        a.GetComponent<RectTransform>().anchoredPosition = new Vector2(-230, 137 + -(num * 35));

        a.transform.Find("Label").GetComponent<Text>().text = property.Name;

        if (property is SimulationBoolProperty)
        {
            var toggle = a.GetComponentInChildren<Toggle>();

            toggle.isOn = ((SimulationBoolProperty)property).Value;
            toggle.GetComponentInChildren<Text>().text = string.Empty;
            toggle.onValueChanged.AddListener((v) => ((SimulationBoolProperty)property).Value = v);
        }
        else
        {
            var input = a.GetComponentInChildren<InputField>();

            input.text = property.GetValue();
            input.onValueChanged.AddListener(new InputWrapper(input, property).OnChange);
        }
        num++;
    }

    private static Transform GetPropertiesContentArea()
    {
        var properties = GameObject.Find("Properties");
        var content = properties.transform.Find("Viewport").Find("Content");
        return content;
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
