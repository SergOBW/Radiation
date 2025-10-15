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
    [Header("Input Actions (из XRI Default Input Actions)")]
    [Tooltip("Обычно: XRI LeftHand Interaction / Select")]
    [SerializeField] private InputActionReference leftSelect;
    [Tooltip("Обычно: XRI RightHand Interaction / Select")]
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
        // Логи выбора — удобно для диагностики
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

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        Debug.Log($"[VRSignalOnSelectActionWhileSelected] SELECT ENTER -> '{name}', interactor: {args.interactorObject}");
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        Debug.Log($"[VRSignalOnSelectActionWhileSelected] SELECT EXIT  -> '{name}', interactor: {args.interactorObject}");
    }

#if ENABLE_INPUT_SYSTEM
    private void SubscribeActions()
    {
        if (_subscribed) return;

        if (leftSelect && leftSelect.action != null)
            leftSelect.action.performed += OnLeftPerformed;

        if (rightSelect && rightSelect.action != null)
            rightSelect.action.performed += OnRightPerformed;

        _subscribed = true;
        Debug.Log($"[VRSignalOnSelectActionWhileSelected] Subscribed to Select actions (L:'{leftSelect?.action?.name ?? "null"}', R:'{rightSelect?.action?.name ?? "null"}')");
    }

    private void UnsubscribeActions()
    {
        if (!_subscribed) return;

        if (leftSelect && leftSelect.action != null)
            leftSelect.action.performed -= OnLeftPerformed;

        if (rightSelect && rightSelect.action != null)
            rightSelect.action.performed -= OnRightPerformed;

        _subscribed = false;
        Debug.Log("[VRSignalOnSelectActionWhileSelected] Unsubscribed from Select actions");
    }

    private void OnLeftPerformed(InputAction.CallbackContext _)  => TryEmit("Left");
    private void OnRightPerformed(InputAction.CallbackContext _) => TryEmit("Right");
#endif

    private void TryEmit(string hand)
    {
        // Шлём сигнал только если объект ВЫБРАН (держится интерактором)
        if (_interactable != null && _interactable.isHovered)
        {
            Debug.Log($"[VRSignalOnSelectActionWhileSelected] {hand} Select -> EMIT '{signalOnPress}' for '{name}'");
            if (!string.IsNullOrWhiteSpace(signalOnPress))
                _orchestrator?.Signals.Emit(signalOnPress);
        }
        else
        {
            Debug.Log($"[VRSignalOnSelectActionWhileSelected] {hand} Select -> объект НЕ выбран, сигнал НЕ отправлен ('{name}')");
        }
    }

    // ===== Публичные методы для репозитория =====
#if ENABLE_INPUT_SYSTEM
    public void ConfigureInputActions(InputActionReference left, InputActionReference right)
    {
        bool wasActive = isActiveAndEnabled;
        if (wasActive) UnsubscribeActions();

        leftSelect = left;
        rightSelect = right;

        if (wasActive) SubscribeActions();
        Debug.Log($"[VRSignalOnSelectActionWhileSelected] Actions configured (L:'{leftSelect?.action?.name ?? "null"}', R:'{rightSelect?.action?.name ?? "null"}')");
    }
#endif

    public void SetOrchestrator(ConversationOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }
}
