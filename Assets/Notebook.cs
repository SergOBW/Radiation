using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

[DisallowMultipleComponent]
public class Notebook : MonoBehaviour
{
    private readonly Dictionary<string, float> _values = new();
    private readonly HashSet<string> _completed = new();

    [Header("Signals")]
    [Tooltip("Префикс динамического сигнала: <Prefix>:<PointName>")]
    [SerializeField] private string dynamicPrefix = "Notebook.Recorded";

    [Tooltip("Если true, дополнительно шлём общий сигнал")]
    [SerializeField] private bool emitCommonSignal = true;

    [Inject] private SceneSignalHub _sceneSignalHub;
    [Inject] private ScenarioSignalHub _scenarioSignalHub;

    public event Action Updated;

    public void SetValue(string pointName, float value)
    {
        if (string.IsNullOrWhiteSpace(pointName))
            pointName = "Unnamed";

        _values[pointName] = value;
        _completed.Add(pointName);

        Updated?.Invoke();

        EmitSignals(pointName);
    }

    public bool TryGetValue(string pointName, out float value)
        => _values.TryGetValue(pointName, out value);

    public bool IsCompleted(string pointName)
        => _completed.Contains(pointName);

    private void EmitSignals(string pointName)
    {
        // Собираем динамическое имя сигнала: Prefix:PointName
        string dyn =
        $"{dynamicPrefix}:{pointName}";

        bool haveSceneHub = _sceneSignalHub != null;
        bool haveScenarioHub = _scenarioSignalHub != null;

        if (!haveSceneHub && !haveScenarioHub)
        {
            Debug.LogWarning("[Notebook] Signal hubs are not injected. Skipping signal emit.");
            return;
        }

        // Динамический сигнал
        if (haveSceneHub)    _sceneSignalHub.EmitAll(dyn);
        if (haveScenarioHub) _scenarioSignalHub.Emit(dyn);
    }
}
