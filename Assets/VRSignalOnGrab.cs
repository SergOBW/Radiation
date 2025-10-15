using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DisallowMultipleComponent]
[RequireComponent(typeof(XRGrabInteractable))]
public sealed class VRSignalOnGrab : MonoBehaviour
{
    [Header("Signals")]
    [SerializeField] private string signalOnGrab = "PickedUp";
    [SerializeField] private string signalOnRelease = "Released";

    private XRGrabInteractable _grab;
    private ConversationOrchestrator _orchestrator;

    public void Initialize(ConversationOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
        Register();
    }

    private void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable() => Register();
    private void OnDisable() => Unregister();

    private void Register()
    {
        if (_grab == null) return;

        _grab.selectEntered.AddListener(OnGrab);
        _grab.selectExited.AddListener(OnRelease);
    }

    private void Unregister()
    {
        if (_grab == null) return;

        _grab.selectEntered.RemoveListener(OnGrab);
        _grab.selectExited.RemoveListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs _)
    {
        if (!string.IsNullOrWhiteSpace(signalOnGrab))
            _orchestrator.Signals.Emit(signalOnGrab);
    }

    private void OnRelease(SelectExitEventArgs _)
    {
        if (!string.IsNullOrWhiteSpace(signalOnRelease))
            _orchestrator.Signals.Emit(signalOnRelease);
    }
}