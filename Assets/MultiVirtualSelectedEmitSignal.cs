using System.Collections.Generic;
using UnityEngine;
using VContainer;

[DisallowMultipleComponent]
public sealed class MultiVirtualSelectedEmitSignal : MonoBehaviour
{
    [Header("Части (с SelectByTriggerOnHover)")]
    [SerializeField] private List<SelectByTriggerOnHover> parts = new List<SelectByTriggerOnHover>();

    [Header("Сигнал")]
    [SerializeField] private string signalWhenAllSelected = "RadiometerAllPartsSelected";

    [Header("Поведение")]
    [Tooltip("Слать сигнал один раз за цикл: после сброса (когда какая-то часть стала не выделена) можно будет слать снова.")]
    [SerializeField] private bool fireOncePerLatch = true;

    [Inject] private SceneSignalHub _sceneSignalHub;
    [Inject] private ScenarioSignalHub _scenarioSignalHub;

    private bool _latched;

    private void OnEnable()
    {
        foreach (var part in parts)
        {
            if (part != null)
                part.OnVirtualSelectionChanged += OnSelChanged;
        }
        Reevaluate();
    }

    private void OnDisable()
    {
        foreach (var part in parts)
        {
            if (part != null)
                part.OnVirtualSelectionChanged -= OnSelChanged;
        }
    }

    private void OnSelChanged(SelectByTriggerOnHover _, bool __) => Reevaluate();

    private void Reevaluate()
    {
        if (parts.Count == 0) return;

        // Проверяем, выбраны ли все части
        bool allSelected = true;

        foreach (var part in parts)
        {
            if (part == null || !part.IsSelectedVirtually)
            {
                allSelected = false;
                break;
            }
        }

        if (allSelected)
        {
            if (!fireOncePerLatch || !_latched)
            {
                Emit();
                _latched = true;
            }
        }
        else
        {
            _latched = false;
        }
    }

    private void Emit()
    {
        if (!string.IsNullOrWhiteSpace(signalWhenAllSelected))
        {
            bool haveSceneHub = _sceneSignalHub != null;
            bool haveScenarioHub = _scenarioSignalHub != null;

            if (!haveSceneHub && !haveScenarioHub)
            {
                Debug.LogWarning("[SelectedPointsSignalEmitter] No signal hubs injected.");
                return;
            }

            if (haveSceneHub) _sceneSignalHub.EmitAll(signalWhenAllSelected);
            if (haveScenarioHub) _scenarioSignalHub.Emit(signalWhenAllSelected);

            Debug.Log($"[MultiVirtualSelectedEmitSignal] Emitted: {signalWhenAllSelected}");
        }
    }

    public void SetParts(List<SelectByTriggerOnHover> newParts)
    {
        OnDisable();
        parts = newParts ?? new List<SelectByTriggerOnHover>();
        _latched = false;
        OnEnable();
    }
}
