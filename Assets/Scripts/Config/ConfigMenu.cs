﻿#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Assets.Scripts.Config;
using UnityEngine.UI;
using System.IO;
using System.Xml.Serialization;
using System;
using System.Collections.Generic;
using Assets.Scripts.Extensions;
using Assets.Scripts.Arenas;

public class ConfigMenu : MonoBehaviour, IDisposable
{
    public SimulationSettings Settings { get; set; }

    private List<string> _batchExperimentPaths;
    private StreamWriter _batchLog;

    private InputField _experimentFileInput;
    private InputField _arenaFileInput;

    private Button _btnStart;

    void Start()
    {
        _btnStart = this.ButtonByName("btnStart");
        _btnStart.onClick.AddListener(Start_Clicked);

        var batchPath = @"C:\Users\andos\git\space\Batches\Test";
        batchPath = null;

        if (string.IsNullOrEmpty(batchPath))
        {
            RunInRegularMode();
        }
        else
        {
            RunInBatchMode(batchPath);
        }
    }

    private void RunInBatchMode(string batchPath)
    {
        DontDestroyOnLoad(this);

        transform.DetachChildren();

        _batchExperimentPaths = new List<string>();
        var experiments = Directory.GetFiles(batchPath, "*.xml");

        _batchLog = new StreamWriter(Path.Combine(batchPath, "log.txt"));

        _batchLog.WriteLine("Found " + experiments.Length + " experiments");

        _batchExperimentPaths.AddRange(experiments);
    }

    private void RunInRegularMode()
    {
#if !UNITY_WEBGL
        _experimentFileInput = GameObject.Find("pnlSaveLoad").gameObject.InputByName("txtFile");
        _arenaFileInput = GameObject.Find("pnlArena").gameObject.InputByName("txtFile");

        _experimentFileInput.onValueChanged.AddListener(s => ValidateFile(s, _experimentFileInput));
        _arenaFileInput.onValueChanged.AddListener(s => ValidateFile(s, _arenaFileInput));

        var file = PlayerPrefs.GetString("ExperimentFile");

        if (!string.IsNullOrEmpty(file))
        {
            try
            {
                LoadExperimentFromFile(file);
            }
            catch { }
        }
        if (Settings == null)
        {
            LoadSimulationSettings(new SimulationSettings());
        }

        var saveExperiment = GameObject.Find("pnlSaveLoad").ButtonByName("Save");
        saveExperiment.onClick.AddListener(SaveExperiment_Clicked);
        var loadExperiment = GameObject.Find("pnlSaveLoad").ButtonByName("Load");
        loadExperiment.onClick.AddListener(LoadExperiment_Clicked);

        var batch = GameObject.Find("pnlSaveLoad").ButtonByName("Batch");
        batch.onClick.AddListener(Batch_Clicked);

        var loadArena = GameObject.Find("pnlArena").ButtonByName("Load");
        loadArena.onClick.AddListener(LoadArena_Clicked);

        ValidateArena();

#else
        GameObject.Find("pnlSaveLoad").transform.parent = null;
        GameObject.Find("pnlArena").transform.parent = null;
#endif
    }

    private void ValidateFile(string s, InputField input)
    {
#if !UNITY_WEBGL
        input.SetColour(File.Exists(s) ? Color.black : Color.red);

        _btnStart.interactable = File.Exists(_experimentFileInput.text) && File.Exists(_experimentFileInput.text);
#endif
    }

    private void LoadSimulationSettings(SimulationSettings settings)
    {
        Settings = settings;

        try
        {
            var dropDown = GameObject.Find("LevelSelect").GetComponent<Dropdown>();
            foreach (var option in dropDown.options)
                if (option.text == Settings.ArenaName)
                    dropDown.value = dropDown.options.IndexOf(option);
        }
        catch { }

        num = 0;
        max = Settings.AllProperties.Count;
        GetPropertiesContentArea().DetachChildren();
        foreach (var v in Settings.AllProperties)
            CreateInput(v);

        var rect = GetPropertiesContentArea().GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(rect.rect.width, 35 * Settings.AllProperties.Count);

        Screen.SetResolution(Screen.width + 1, Screen.height, Screen.fullScreen, Screen.currentResolution.refreshRate);

        ValidateArena();
    }

    private void ValidateArena()
    {
#if !UNITY_WEBGL
        var fileName = string.IsNullOrEmpty(Settings.ArenaFilename) ? string.Empty : Settings.ArenaFilename;

        _arenaFileInput.text = fileName;

        if (File.Exists(_arenaFileInput.text))
        {
            _btnStart.interactable = true;
            _arenaFileInput.SetColour(Color.black);
        }
        else
        {
            _btnStart.interactable = false;
            _arenaFileInput.SetColour(Color.red);
        }
#endif
    }

    private void LoadExperiment_Clicked()
    {
#if UNITY_EDITOR
        var file = EditorUtility.OpenFilePanel("Load File", string.Empty, "xml");
        _experimentFileInput.text = file;
#endif

        LoadExperimentFromFile(_experimentFileInput.text);
    }

    private void Batch_Clicked()
    {
#if UNITY_EDITOR
        var file = EditorUtility.OpenFilePanel("Load File", string.Empty, string.Empty);
        _experimentFileInput.text = file;
#endif

        var fileToRun = _experimentFileInput.text;
        if (!Directory.Exists(_experimentFileInput.text))
            fileToRun = Path.GetDirectoryName(_experimentFileInput.text);
        RunInBatchMode(fileToRun);
    }

    private void LoadExperimentFromFile(string file)
    {
#if !UNITY_WEBGL
        PlayerPrefs.SetString("ExperimentFile", file);

        _experimentFileInput.text = file;

        using (var sr = new StreamReader(_experimentFileInput.text))
        {
            var xml = new XmlSerializer(typeof(SimulationSettings));

            var settings = xml.Deserialize(sr) as SimulationSettings;

            if (settings != null)
            {
                LoadSimulationSettings(settings);
            }
        }
#endif
    }

    private void SaveExperiment_Clicked()
    {
#if UNITY_EDITOR
        var file = EditorUtility.SaveFilePanel("Save File", string.Empty, "space.xml", "xml");
        _experimentFileInput.text = file;
#endif

#if !UNITY_WEBGL
        using (var sr = new StreamWriter(_experimentFileInput.text))
        {
            var xml = new XmlSerializer(typeof(SimulationSettings));

            xml.Serialize(sr, Settings);
        }
#endif
    }

    private void LoadArena_Clicked()
    {
#if UNITY_EDITOR
        var file = EditorUtility.OpenFilePanel("Load File", string.Empty, "xml");
        _arenaFileInput.text = file;
#endif

        Settings.ArenaFilename = _arenaFileInput.text;
        ValidateArena();
    }

    private void Start_Clicked()
    {
        var go = new GameObject("Arena Loader");
        GameObject.DontDestroyOnLoad(go);
        go.AddComponent<ArenaLoader>();
        go.GetComponent<ArenaLoader>().Load(Settings);
    }

    int max;
    int num = 0;

    private void CreateInput(SimulationPropertyBase property)
    {
        Transform content = GetPropertiesContentArea();

        var inputControl = (property is SimulationBoolProperty ? Resources.Load("InputBool") : Resources.Load("InputText")) as GameObject;

        var a = GameObject.Instantiate(inputControl);

        a.transform.SetParent(content.transform);

        //a.transform.position = new Vector3(-240, 45 + -(0 + (num * 35)), 0);
        //a.transform.position = new Vector3(-240, 45 + (0 + (num * 35)), 0);
        a.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -((num + 1) * 35) + 13);
        a.GetComponent<RectTransform>().pivot = new Vector2(0, .5f);

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
        if (ArenaLoader.Loading)
            return;

        if (_batchExperimentPaths != null)
        {
            if (_batchExperimentPaths.Count == 0)
            {
                WriteLine("Finished batch");
                Application.Quit();
            }
            else
            {
                // If there is no running simulation then load the next one
                if (SimulationManager.Instance == null || !SimulationManager.Instance.SimulationRunning)
                {
                    if (SimulationManager.Instance != null)
                        WriteLine("Done");

                    var experiment = _batchExperimentPaths[0];
                    _batchExperimentPaths.RemoveAt(0);
                    SimulationSettings settings = null;

                    try
                    {
                        using (StreamReader sr = new StreamReader(experiment))
                        {
                            XmlSerializer xml = new XmlSerializer(typeof(SimulationSettings));

                            settings = xml.Deserialize(sr) as SimulationSettings;
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteLine("Error loading " + experiment + " - " + ex.Message);
                    }

                    if (settings == null)
                    {
                        WriteLine("Unable to load " + experiment + " - skipping");
                    }
                    else
                    {
                        var batchPath = Path.GetFileName(Path.GetDirectoryName(experiment));

                        settings.ExperimentName.Value = Path.Combine(batchPath, settings.ExperimentName.Value);

                        if (Directory.Exists(Path.Combine("Results", settings.ExperimentName.Value)))
                        {
                            WriteLine("Skipping" + experiment);
                        }
                        else
                        {
                            WriteLine("Running " + experiment);

                            var go = new GameObject("Arena Loader - " + Path.GetFileName(settings.ExperimentName.Value));
                            GameObject.DontDestroyOnLoad(go);
                            go.AddComponent<ArenaLoader>();
                            go.GetComponent<ArenaLoader>().Load(settings);
                        }
                    }
                }
            }
        }
    }

    private void WriteLine(string v)
    {
        if (_batchLog != null)
            _batchLog.WriteLine(v);
        Debug.Log(v);
    }

    public void Dispose()
    {
        if (_batchLog != null)
            _batchLog.Close();
    }
}
