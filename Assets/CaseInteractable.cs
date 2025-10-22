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

    [Header("Сигналы")]
    [SerializeField] private string signalOnOpened = "CaseOpened";
    [SerializeField] private string signalOnClosed = "CaseClosed";

    [Header("Отправлять сигнал только после полного открытия/закрытия")]
    [SerializeField] private bool emitOnlyWhenFullyReached = false;
    [SerializeField] private float reachedEpsilonDegrees = 2f;

#if ENABLE_INPUT_SYSTEM
    [Header("Input Actions")]
    [SerializeField] private InputActionReference leftSelect;
    [SerializeField] private InputActionReference rightSelect;
    [Header("Антидребезг")]
    [SerializeField] private float toggleCooldown = 0.15f;
#endif

    private XRGrabInteractable _grab;
    private bool _isOpen;
    private bool _isHeld;
    private float _lastToggleTime;
    private bool _subscribed;

    private bool _pendingEmitOpen;
    private bool _pendingEmitClose;

    [Inject] private ScenarioSignalHub _scenarioHub;
    [Inject] private SceneSignalHub _sceneHub;

    private void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
        _grab.selectEntered.AddListener(_ => _isHeld = true);
        _grab.selectExited.AddListener(_ => _isHeld = false);
    }

    private void OnDestroy()
    {
        _grab.selectEntered.RemoveAllListeners();
        _grab.selectExited.RemoveAllListeners();
        UnsubscribeInput();
    }

    private void OnEnable()  => SubscribeInput();
    private void OnDisable() => UnsubscribeInput();

    private void Update()
    {
        if (!lid) return;

        var target = Quaternion.Euler(_isOpen ? openedRotation : closedRotation);
        lid.localRotation = Quaternion.Slerp(lid.localRotation, target, Time.deltaTime * openSpeed);

        if (emitOnlyWhenFullyReached)
        {
            float angle = Quaternion.Angle(lid.localRotation, target);
            if (angle <= reachedEpsilonDegrees)
            {
                if (_pendingEmitOpen)
                {
                    _pendingEmitOpen = false;
                    EmitBoth(signalOnOpened);
                }
                else if (_pendingEmitClose)
                {
                    _pendingEmitClose = false;
                    EmitBoth(signalOnClosed);
                }
            }
        }
    }

#if ENABLE_INPUT_SYSTEM
    private void SubscribeInput()
    {
        if (_subscribed) return;
        if (leftSelect?.action != null)
        {
            if (!leftSelect.action.enabled) leftSelect.action.Enable();
            leftSelect.action.performed += OnTriggerPerformed;
        }
        if (rightSelect?.action != null)
        {
            if (!rightSelect.action.enabled) rightSelect.action.Enable();
            rightSelect.action.performed += OnTriggerPerformed;
        }
        _subscribed = true;
    }

    private void UnsubscribeInput()
    {
        if (!_subscribed) return;
        if (leftSelect?.action != null)
            leftSelect.action.performed -= OnTriggerPerformed;
        if (rightSelect?.action != null)
            rightSelect.action.performed -= OnTriggerPerformed;
        _subscribed = false;
    }

    private void OnTriggerPerformed(InputAction.CallbackContext ctx)
    {
        if (!_isHeld) return;
        if (Time.unscaledTime - _lastToggleTime < toggleCooldown) return;
        _lastToggleTime = Time.unscaledTime;
        ToggleCase();
    }
#endif

    private void ToggleCase()
    {
        _isOpen = !_isOpen;
        Debug.Log($"[Case] Состояние: {(_isOpen ? "Открыт" : "Закрыт")}");

        if (emitOnlyWhenFullyReached)
        {
            _pendingEmitOpen  = _isOpen;
            _pendingEmitClose = !_isOpen;
        }
        else
        {
            if (_isOpen) EmitBoth(signalOnOpened);
            else         EmitBoth(signalOnClosed);
        }
    }

    private void EmitBoth(string signal)
    {
        if (string.IsNullOrWhiteSpace(signal)) return;

        // сразу в оба хаба
        _scenarioHub?.Emit(signal);
        _sceneHub?.EmitAll(signal);

        Debug.Log($"[Case] EmitBoth → '{signal}'");
    }
}
