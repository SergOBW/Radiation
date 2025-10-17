using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VContainer;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(XRBaseInteractable))]
public sealed class VRSignalOnSelectActionWhileSelected : MonoBehaviour
{
    [Header("Сигнал")]
    [SerializeField] private string signalOnPress = "SelectedTrigger";

#if ENABLE_INPUT_SYSTEM
    [Header("Input Actions")]
    [SerializeField] private InputActionReference leftSelect;
    [SerializeField] private InputActionReference rightSelect;
#endif

    private XRBaseInteractable _interactable;
    [Inject] private ConversationOrchestrator _orchestrator;
    private bool _subscribed;

    private void Awake()
    {
        _interactable = GetComponent<XRBaseInteractable>();
    }

    private void OnEnable()
    {
        _interactable.selectEntered.AddListener(OnSelectEntered);
        _interactable.selectExited.AddListener(OnSelectExited);

#if ENABLE_INPUT_SYSTEM
        SubscribeActions();
#endif
    }

    private void OnDisable()
    {
        _interactable.selectEntered.RemoveListener(OnSelectEntered);
        _interactable.selectExited.RemoveListener(OnSelectExited);

#if ENABLE_INPUT_SYSTEM
        UnsubscribeActions();
#endif
    }

    private void OnSelectEntered(SelectEnterEventArgs args) { }

    private void OnSelectExited(SelectExitEventArgs args) { }

#if ENABLE_INPUT_SYSTEM
    private void SubscribeActions()
    {
        if (_subscribed) return;

        if (leftSelect && leftSelect.action != null)
            leftSelect.action.performed += OnLeftPerformed;

        if (rightSelect && rightSelect.action != null)
            rightSelect.action.performed += OnRightPerformed;

        _subscribed = true;
    }

    private void UnsubscribeActions()
    {
        if (!_subscribed) return;

        if (leftSelect && leftSelect.action != null)
            leftSelect.action.performed -= OnLeftPerformed;

        if (rightSelect && rightSelect.action != null)
            rightSelect.action.performed -= OnRightPerformed;

        _subscribed = false;
    }

    private void OnLeftPerformed(InputAction.CallbackContext context)
    {
        TryEmit();
    }

    private void OnRightPerformed(InputAction.CallbackContext context)
    {
        TryEmit();
    }
#endif

    private void TryEmit()
    {
        if (_interactable != null && _interactable.isHovered)
        {
            if (!string.IsNullOrWhiteSpace(signalOnPress))
                _orchestrator?.Signals.Emit(signalOnPress);
        }
    }

#if ENABLE_INPUT_SYSTEM
    public void ConfigureInputActions(InputActionReference left, InputActionReference right)
    {
        bool wasActive = isActiveAndEnabled;
        if (wasActive) UnsubscribeActions();

        leftSelect = left;
        rightSelect = right;

        if (wasActive) SubscribeActions();
    }
#endif

    public void SetOrchestrator(ConversationOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }
}
