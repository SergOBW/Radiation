using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
// === Кастомный инспектор с кнопкой ===
[CustomEditor(typeof(RadiationSurveyManager))]
public class RadiationSurveyManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var mgr = (RadiationSurveyManager)target;

        GUILayout.Space(10);
        GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
        if (GUILayout.Button("🔄 Обновить список зон"))
        {
            mgr.RescanZones();
        }
        GUI.backgroundColor = Color.white;
    }
}
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class RadiationSurveyManager : MonoBehaviour
{
    [SerializeField] private DosimeterCore dosimeterCore;
    public Notebook notebook;

    [Header("Зоны (перетаскивай и меняй порядок вручную)")]
    public List<RadiationSampleZone> zones = new();
    public event Action AllCompleted;
    public event Action PointsListChanged;
    public event Action<string> PointCompleted;

    private int _total, _completed;
    private bool _fired;
    private Dictionary<RadiationSampleZone, float> accumDose = new();
    private Dictionary<RadiationSampleZone, float> accumTime = new();
    private HashSet<RadiationSampleZone> completedZones = new();

    private void OnEnable()
    {
        if (!Application.isPlaying) RescanZones();
    }

    private void Start()
    {
        InitializeMeasurement();
        PointsListChanged?.Invoke();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) RescanZones();
    }

    public void RescanZones()
    {
        var found = new List<RadiationSampleZone>(FindObjectsByType<RadiationSampleZone>(FindObjectsSortMode.None));
        found.RemoveAll(z => z == null);

        zones.RemoveAll(z => z == null);

        var existing = new HashSet<RadiationSampleZone>(zones);
        foreach (var z in found)
            if (!existing.Contains(z))
                zones.Add(z);

        var foundSet = new HashSet<RadiationSampleZone>(found);
        zones.RemoveAll(z => !foundSet.Contains(z));

        InitializeMeasurement();

#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.SetDirty(this);
#endif
        PointsListChanged?.Invoke();
    }

    private void InitializeMeasurement()
    {
        _total = zones.Count;
        _completed = 0;
        _fired = false;
        accumDose.Clear();
        accumTime.Clear();
        completedZones.Clear();
        foreach (var z in zones)
        {
            accumDose[z] = 0f;
            accumTime[z] = 0f;
            z.IsCompleted = false;
            if (z.gameObject.activeSelf == false) z.gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        if (_fired || zones.Count == 0 || dosimeterCore == null || dosimeterCore.CurrentSensor == null) return;

        var sensor = dosimeterCore.CurrentSensor;
        if (!sensor.IsWorking || sensor.probePoint == null)
        {
            ResetAccumulations();
            return;
        }

        Vector3 probePos = sensor.probePoint.position;
        float currentDose = sensor.CurrentDoseRateMicroSvPerHour;

        foreach (var zone in zones)
        {
            if (completedZones.Contains(zone)) continue;

            if (!zone.IsInsideAnyCollider(probePos, 0.05f))
            {
                accumDose[zone] = 0f;
                accumTime[zone] = 0f;
                continue;
            }

            bool modeMatchesChannel = false;
            switch (dosimeterCore.ActiveSlot)
            {
                case SensorSlot.Gamma:
                    modeMatchesChannel = (zone.measureChannels & RadiationChannel.Gamma) != 0;
                    break;
                case SensorSlot.Betta:
                    modeMatchesChannel = (zone.measureChannels & RadiationChannel.Beta) != 0;
                    break;
            }

            if (!modeMatchesChannel)
            {
                accumDose[zone] = 0f;
                accumTime[zone] = 0f;
                continue;
            }

            if ((sensor.sensitivity & zone.measureChannels) == 0)
            {
                accumDose[zone] = 0f;
                accumTime[zone] = 0f;
                continue;
            }

            accumDose[zone] += currentDose * Time.deltaTime;
            accumTime[zone] += Time.deltaTime;

            if (accumTime[zone] >= zone.measurementTime)
            {
                float avg = accumDose[zone] / Mathf.Max(accumTime[zone], 0.0001f);
                RecordMeasurement(zone, avg);
            }
        }
    }


    private void ResetAccumulations()
    {
        foreach (var zone in zones)
        {
            accumDose[zone] = 0f;
            accumTime[zone] = 0f;
        }
    }

    private void RecordMeasurement(RadiationSampleZone zone, float peak)
    {
        if (completedZones.Contains(zone)) return;

        completedZones.Add(zone);
        _completed++;
        zone.CompleteAndDeactivate();

        if (notebook != null)
        {
            if (notebook.TryGetValue(zone.pointName, out float prev))
            {
                if (peak > prev)
                    notebook.SetValue(zone.pointName, peak);
            }
            else
            {
                notebook.SetValue(zone.pointName, peak);
            }
        }

        PointCompleted?.Invoke(zone.pointName);

        if (!_fired && _completed >= _total)
        {
            _fired = true;
            AllCompleted?.Invoke();
        }
    }

    public bool IsPointCompleted(string pointName)
    {
        return notebook != null && notebook.IsCompleted(pointName);
    }
}
