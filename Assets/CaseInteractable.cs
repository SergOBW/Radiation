using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using VContainer;

[DisallowMultipleComponent]
[RequireComponent(typeof(XRGrabInteractable))]
public sealed class CaseInteractable : MonoBehaviour
{
    [Header("Крышка кейса")]
    [SerializeField] private Transform lid;
    [SerializeField] private Vector3 closedRotation = Vector3.zero;
    [SerializeField] private Vector3 openedRotation = new Vector3(-120f, 0f, 0f);
    [SerializeField] private float openSpeed = 5f;

    [Header("Сигналы оркестратору")]
    [SerializeField] private string signalOnOpened = "CaseOpened";
    [SerializeField] private string signalOnClosed = "CaseClosed";

    [Tooltip("Если включено — сигнал шлётся только когда крышка докрутилась до целевого угла.")]
    [SerializeField] private bool emitOnlyWhenFullyReached = false;

    [Tooltip("Допуск по углу (в градусах) для 'докрутилась'.")]
    [SerializeField] private float reachedEpsilonDegrees = 2f;

#if ENABLE_INPUT_SYSTEM
    [Header("Input Actions")]
    [Tooltip("Экшен триггера левой руки (performed)")]
    [SerializeField] private InputActionReference leftSelect;
    [Tooltip("Экшен триггера правой руки (performed)")]
    [SerializeField] private InputActionReference rightSelect;

    [Header("Антидребезг")]
    [SerializeField] private float toggleCooldown = 0.15f;
#endif

    private XRGrabInteractable _grab;
    private bool _isOpen;
    private bool _isHeld;
    private float _lastToggleTime;
    private bool _subscribed;

    // ожидание эмита, если включён режим "emitOnlyWhenFullyReached"
    private bool _pendingEmitOpen;
    private bool _pendingEmitClose;

    [Inject] private ConversationOrchestrator _orchestrator;

    private void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
        _grab.selectEntered.AddListener(OnGrab);
        _grab.selectExited.AddListener(OnRelease);
    }

    private void OnDestroy()
    {
        _grab.selectEntered.RemoveListener(OnGrab);
        _grab.selectExited.RemoveListener(OnRelease);
        UnsubscribeInput();
    }

    private void OnEnable()
    {
        SubscribeInput();
    }

    private void OnDisable()
    {
        UnsubscribeInput();
    }

    private void OnGrab(SelectEnterEventArgs _) => _isHeld = true;
    private void OnRelease(SelectExitEventArgs _) => _isHeld = false;

    private void Update()
    {
        if (!lid) return;

        // Плавный поворот
        var target = Quaternion.Euler(_isOpen ? openedRotation : closedRotation);
        lid.localRotation = Quaternion.Slerp(lid.localRotation, target, Time.deltaTime * openSpeed);

        // Если нужно слать сигнал по факту достижения целевого угла — проверяем
        if (emitOnlyWhenFullyReached)
        {
            float angle = Quaternion.Angle(lid.localRotation, target);
            if (angle <= reachedEpsilonDegrees)
            {
                if (_pendingEmitOpen)
                {
                    _pendingEmitOpen = false;
                    EmitSignalOpened();
                }
                else if (_pendingEmitClose)
                {
                    _pendingEmitClose = false;
                    EmitSignalClosed();
                }
            }
        }
    }

#if ENABLE_INPUT_SYSTEM
    private void SubscribeInput()
    {
        if (_subscribed) return;

        if (leftSelect && leftSelect.action != null)
        {
            if (!leftSelect.action.enabled) leftSelect.action.Enable();
            leftSelect.action.performed += OnAnySelectPerformed;
        }

        if (rightSelect && rightSelect.action != null)
        {
            if (!rightSelect.action.enabled) rightSelect.action.Enable();
            rightSelect.action.performed += OnAnySelectPerformed;
        }

        _subscribed = true;
    }

    private void UnsubscribeInput()
    {
        if (!_subscribed) return;

        if (leftSelect && leftSelect.action != null)
            leftSelect.action.performed -= OnAnySelectPerformed;

        if (rightSelect && rightSelect.action != null)
            rightSelect.action.performed -= OnAnySelectPerformed;

        _subscribed = false;
    }

    private void OnAnySelectPerformed(InputAction.CallbackContext ctx)
    {
        if (!_isHeld) return;
        if (Time.unscaledTime - _lastToggleTime < toggleCooldown) return;

        _lastToggleTime = Time.unscaledTime;
        ToggleCase();
    }

    public void ConfigureInputActions(InputActionReference left, InputActionReference right)
    {
        bool wasEnabled = isActiveAndEnabled;
        if (wasEnabled) UnsubscribeInput();
        leftSelect = left;
        rightSelect = right;
        if (wasEnabled) SubscribeInput();
    }
#endif

    private void ToggleCase()
    {
        _isOpen = !_isOpen;
        Debug.Log($"[Case] Состояние: {(_isOpen ? "Открыт" : "Закрыт")}");

        if (emitOnlyWhenFullyReached)
        {
            // Отложенная отправка, когда крышка докрутится
            _pendingEmitOpen  = _isOpen;
            _pendingEmitClose = !_isOpen;
        }
        else
        {
            // Мгновенная отправка по факту переключения
            if (_isOpen) EmitSignalOpened();
            else         EmitSignalClosed();
        }
    }

    private void EmitSignalOpened()
    {
        if (!string.IsNullOrWhiteSpace(signalOnOpened))
            _orchestrator?.Signals.Emit(signalOnOpened);
        Debug.Log("[Case] Signal emitted: " + signalOnOpened);
    }

    private void EmitSignalClosed()
    {
        if (!string.IsNullOrWhiteSpace(signalOnClosed))
            _orchestrator?.Signals.Emit(signalOnClosed);
        Debug.Log("[Case] Signal emitted: " + signalOnClosed);
    }

    // Если нужно вручную прокинуть оркестратор без DI:
    public void SetOrchestrator(ConversationOrchestrator orchestrator) => _orchestrator = orchestrator;
}
