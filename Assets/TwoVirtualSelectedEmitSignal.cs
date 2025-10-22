using UnityEngine;
using VContainer;

[DisallowMultipleComponent]
public sealed class TwoVirtualSelectedEmitSignal : MonoBehaviour
{
    [Header("Две части (с SelectByTriggerOnHover)")]
    [SerializeField] private SelectByTriggerOnHover partA;
    [SerializeField] private SelectByTriggerOnHover partB;

    [Header("Сигнал")]
    [SerializeField] private string signalWhenBothSelected = "RadiometerTwoPartsSelected";

    [Header("Поведение")]
    [Tooltip("Слать сигнал один раз за цикл: после сброса (когда какая-то часть стала не выделена) можно будет слать снова.")]
    [SerializeField] private bool fireOncePerLatch = true;

    [Inject] private ConversationOrchestrator _orchestrator;

    private bool _latched;

    private void OnEnable()
    {
        if (partA) partA.OnVirtualSelectionChanged += OnSelChanged;
        if (partB) partB.OnVirtualSelectionChanged += OnSelChanged;
        Reevaluate();
    }

    private void OnDisable()
    {
        if (partA) partA.OnVirtualSelectionChanged -= OnSelChanged;
        if (partB) partB.OnVirtualSelectionChanged -= OnSelChanged;
    }

    private void OnSelChanged(SelectByTriggerOnHover _, bool __) => Reevaluate();

    private void Reevaluate()
    {
        if (!partA || !partB) return;

        bool ok = partA.IsSelectedVirtually && partB.IsSelectedVirtually;

        if (ok)
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
        if (!string.IsNullOrWhiteSpace(signalWhenBothSelected))
        {
            _orchestrator?.scenarioSignals.Emit(signalWhenBothSelected);
            Debug.Log($"[TwoVirtualSelectedEmitSignal] Emitted: {signalWhenBothSelected}");
        }
    }

    public void SetParts(SelectByTriggerOnHover a, SelectByTriggerOnHover b)
    {
        OnDisable();
        partA = a;
        partB = b;
        _latched = false;
        OnEnable();
    }
}
