using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Помечает объект "виртуально выделенным", если на него наведён XR-луч/рука и выполнен триггер (InputActionReference).
/// Подсветку НЕ делает сам — просит OutlineOnHoverXR отобразить "selected"-стиль.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(XRBaseInteractable))]
[RequireComponent(typeof(OutlineOnHoverXR))]
public sealed class SelectByTriggerOnHover : MonoBehaviour
{
#if ENABLE_INPUT_SYSTEM
    [Header("Input Actions (trigger)")]
    [SerializeField] private InputActionReference leftTrigger;
    [SerializeField] private InputActionReference rightTrigger;

    [Header("Антидребезг")]
    [SerializeField] private float pressCooldown = 0.15f;
#endif

    private XRBaseInteractable _interactable;
    private OutlineOnHoverXR _outline;
    private int _hoverCount;
    private float _lastPressTime;
    private bool _subscribed;

    // Публичное состояние
    public bool IsSelectedVirtually => _outline != null && _outline.IsVirtualSelected();
    public System.Action<SelectByTriggerOnHover, bool> OnVirtualSelectionChanged;

    private void Awake()
    {
        _interactable = GetComponent<XRBaseInteractable>();
        _outline = GetComponent<OutlineOnHoverXR>();

        _interactable.hoverEntered.AddListener(OnHoverEntered);
        _interactable.hoverExited.AddListener(OnHoverExited);
    }

    private void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        SubscribeInput();
#endif
    }

    private void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        UnsubscribeInput();
#endif
        _hoverCount = 0;
    }

    private void OnDestroy()
    {
        _interactable.hoverEntered.RemoveListener(OnHoverEntered);
        _interactable.hoverExited.RemoveListener(OnHoverExited);
#if ENABLE_INPUT_SYSTEM
        UnsubscribeInput();
#endif
    }

    private void OnHoverEntered(HoverEnterEventArgs _)
    {
        _hoverCount += 1;
    }

    private void OnHoverExited(HoverExitEventArgs _)
    {
        if (_hoverCount > 0) _hoverCount -= 1;
    }

#if ENABLE_INPUT_SYSTEM
    private void SubscribeInput()
    {
        if (_subscribed) return;

        if (leftTrigger && leftTrigger.action != null)
        {
            if (!leftTrigger.action.enabled) leftTrigger.action.Enable();
            leftTrigger.action.performed += OnAnyTriggerPerformed;
        }
        if (rightTrigger && rightTrigger.action != null)
        {
            if (!rightTrigger.action.enabled) rightTrigger.action.Enable();
            rightTrigger.action.performed += OnAnyTriggerPerformed;
        }

        _subscribed = true;
    }

    private void UnsubscribeInput()
    {
        if (!_subscribed) return;

        if (leftTrigger && leftTrigger.action != null)
            leftTrigger.action.performed -= OnAnyTriggerPerformed;
        if (rightTrigger && rightTrigger.action != null)
            rightTrigger.action.performed -= OnAnyTriggerPerformed;

        _subscribed = false;
    }

    private void OnAnyTriggerPerformed(InputAction.CallbackContext ctx)
    {
        // Срабатываем только если наведен курсор/рука
        if (_hoverCount <= 0) return;

        if (Time.unscaledTime - _lastPressTime < pressCooldown) return;
        _lastPressTime = Time.unscaledTime;

        ToggleVirtualSelection();
    }

    public void ConfigureInputActions(InputActionReference left, InputActionReference right)
    {
        bool wasEnabled = isActiveAndEnabled;
        if (wasEnabled) UnsubscribeInput();

        leftTrigger = left;
        rightTrigger = right;

        if (wasEnabled) SubscribeInput();
    }
#endif

    public void ToggleVirtualSelection()
    {
        SetVirtualSelection(!IsSelectedVirtually);
    }

    public void SetVirtualSelection(bool value)
    {
        bool before = IsSelectedVirtually;
        _outline.SetVirtualSelected(value);
        if (before != value) OnVirtualSelectionChanged?.Invoke(this, value);
    }
}
