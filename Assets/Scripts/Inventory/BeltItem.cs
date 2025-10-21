using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Collider))]
public sealed class BeltItem : XRBaseInteractable
{
    [SerializeField] private Transform beltPoint;     // куда возвращать
    [SerializeField] private Transform leftHandPoint; // куда прикреплять в руке

#if ENABLE_INPUT_SYSTEM
    [SerializeField] private InputActionReference leftSelect;
#endif

    private bool _isHovering;
    private bool _isHeld;

    protected override void OnHoverEntered(HoverEnterEventArgs args)
    {
        base.OnHoverEntered(args);
        _isHovering = true;
    }

    protected override void OnHoverExited(HoverExitEventArgs args)
    {
        base.OnHoverExited(args);
        _isHovering = false;
    }

#if ENABLE_INPUT_SYSTEM
    protected override void OnEnable()
    {
        // ВАЖНО: зарегистрироваться в XRInteractionManager
        base.OnEnable();

        if (leftSelect != null && leftSelect.action != null)
        {
            leftSelect.action.performed += OnPress;
            leftSelect.action.canceled  += OnRelease;
            if (!leftSelect.action.enabled)
                leftSelect.action.Enable();
        }
    }

    protected override void OnDisable()
    {
        if (leftSelect != null && leftSelect.action != null)
        {
            leftSelect.action.performed -= OnPress;
            leftSelect.action.canceled  -= OnRelease;
        }

        // ВАЖНО: корректно отписаться у XRInteractionManager
        base.OnDisable();
    }
#else
    protected override void OnEnable()  { base.OnEnable();  }
    protected override void OnDisable() { base.OnDisable(); }
#endif

#if ENABLE_INPUT_SYSTEM
    private void OnPress(InputAction.CallbackContext _)
    {
        if (_isHeld) return;
        if (!_isHovering) return; // берём только если СНАЧАЛА навели, потом нажали

        transform.SetParent(leftHandPoint, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        _isHeld = true;
        HoldStateBus.Instance?.BeginHold(HandSide.Left);
    }

    private void OnRelease(InputAction.CallbackContext _)
    {
        if (!_isHeld) return;

        transform.SetParent(beltPoint, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        _isHeld = false;
        HoldStateBus.Instance?.EndHold(HandSide.Left);
    }
#endif
}
