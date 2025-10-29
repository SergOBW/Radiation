using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class DosimeterSensorSwitcher : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private DosimeterCore core;
    [SerializeField] private TwoHandItemAutoRight item;

#if ENABLE_INPUT_SYSTEM
    [Header("Input")]
    [SerializeField] private InputActionReference leftTrigger;
#endif

    private bool _pressed;

    protected void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        if (leftTrigger && leftTrigger.action != null)
        {
            leftTrigger.action.performed += OnTriggerPress;
            leftTrigger.action.canceled  += OnTriggerRelease;
            if (!leftTrigger.action.enabled) leftTrigger.action.Enable();
        }
#endif
    }

    protected void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (leftTrigger && leftTrigger.action != null)
        {
            leftTrigger.action.performed -= OnTriggerPress;
            leftTrigger.action.canceled  -= OnTriggerRelease;
        }
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private void OnTriggerPress(InputAction.CallbackContext _)
    {
        if (_pressed) return;
        _pressed = true;

        if (core == null || item == null) return;
        if (!item.InLeftHand) return;

        core.ToggleSensor();
        Debug.Log($"[Dosimeter] Переключён сенсор → {core.ActiveSlot}");
    }

    private void OnTriggerRelease(InputAction.CallbackContext _)
    {
        _pressed = false;
    }
#endif
}