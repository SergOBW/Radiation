using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using VContainer;

[RequireComponent(typeof(Collider))]
public sealed class BeltItem : XRBaseInteractable
{
    [Header("Anchors")]
    [SerializeField] private Transform beltPoint;      // куда возвращать
    [SerializeField] private Transform leftHandPoint;  // куда крепить в руке

#if ENABLE_INPUT_SYSTEM
    [Header("Input")]
    [SerializeField] private InputActionReference leftSelect;   // performed -> взять; canceled -> вернуть
#endif

    [Header("BoolStateHub keys")]
    [SerializeField] private string keyOnBelt  = "Item.MyTool.OnBelt";
    [SerializeField] private string keyInLeft  = "Item.MyTool.InLeftHand";
    [SerializeField] private string keyInAny   = "Item.MyTool.InAnyHand";

    private bool _isHovering;
    private bool _isHeld;

    [Inject] private BoolStateHub _stateHub;

    protected override void OnEnable()
    {
        base.OnEnable();

#if ENABLE_INPUT_SYSTEM
        if (leftSelect != null && leftSelect.action != null)
        {
            leftSelect.action.performed += OnPress;
            leftSelect.action.canceled  += OnRelease;
            if (!leftSelect.action.enabled)
                leftSelect.action.Enable();
        }
#endif
        // дефолт: висит на поясе
        SetStateOnBelt(true);
        SetStateLeft(false);
        SetStateAny(false);
    }

    protected override void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (leftSelect != null && leftSelect.action != null)
        {
            leftSelect.action.performed -= OnPress;
            leftSelect.action.canceled  -= OnRelease;
        }
#endif
        base.OnDisable();
    }

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
    private void OnPress(InputAction.CallbackContext _)
    {
        if (_isHeld) return;
        if (!_isHovering) return; // берём только если навели и затем нажали

        if (leftHandPoint)
        {
            transform.SetParent(leftHandPoint, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        _isHeld = true;
        HoldStateBus.Instance.BeginHold(HandSide.Left);

        // стейты
        SetStateOnBelt(false);
        SetStateLeft(true);
        SetStateAny(true);
    }

    private void OnRelease(InputAction.CallbackContext _)
    {
        if (!_isHeld) return;

        if (beltPoint)
        {
            transform.SetParent(beltPoint, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        _isHeld = false;
        HoldStateBus.Instance.EndHold(HandSide.Left);

        // стейты
        SetStateLeft(false);
        SetStateAny(false);
        SetStateOnBelt(true);
    }
#endif

    private void SetStateOnBelt(bool v) { if (_stateHub == null) return; if (v) _stateHub.SetTrue(keyOnBelt); else _stateHub.SetFalse(keyOnBelt); }
    private void SetStateLeft(bool v)   { if (_stateHub == null) return; if (v) _stateHub.SetTrue(keyInLeft); else _stateHub.SetFalse(keyInLeft); }
    private void SetStateAny(bool v)    { if (_stateHub == null) return; if (v) _stateHub.SetTrue(keyInAny);  else _stateHub.SetFalse(keyInAny); }
}
