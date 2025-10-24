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
    public Notebook notebook;

    [Header("Зоны (перетаскивай и меняй порядок вручную)")]
    public List<RadiationSampleZone> zones = new();

    public event Action AllCompleted;
    public event Action PointsListChanged;

    public int Total => _total;
    public int Completed => _completed;

    private int _total, _completed;
    private bool _fired;

    private void OnEnable()
    {
        if (!Application.isPlaying) RescanZones();
    }

    private void Start()
    {
        WireZones();
        PointsListChanged?.Invoke();
    }

    private void OnDestroy()
    {
        UnwireZones();
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

#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        PointsListChanged?.Invoke();
    }

    private void WireZones()
    {
        UnwireZones();
        _total = 0; _completed = 0; _fired = false;

        foreach (var z in zones)
        {
            if (z == null) continue;
            _total++;
            z.Measured += OnZoneMeasured;
        }
    }

    private void UnwireZones()
    {
        foreach (var z in zones)
            if (z != null)
                z.Measured -= OnZoneMeasured;
    }

    // Теперь трактуем входной параметр как ПИК (максимум), а в блокнот записываем максимум из имеющегося и поступившего
    private void OnZoneMeasured(RadiationSampleZone zone, float peak)
    {
        if (notebook != null)
        {
            if (notebook.TryGetValue(zone.pointName, out float prev))
            {
                if (peak > prev)
                    notebook.SetValue(zone.pointName, peak);
                // если новый меньше/равен — оставляем прежний максимум, ничего не пишем
            }
            else
            {
                // первая запись по зоне — просто сохраняем
                notebook.SetValue(zone.pointName, peak);
            }
        }

        _completed++;

        if (!_fired && _completed >= _total)
        {
            _fired = true;
            AllCompleted?.Invoke();
        }
    }

    public bool IsPointCompleted(string pointName)
        => notebook != null && notebook.IsCompleted(pointName);
}
