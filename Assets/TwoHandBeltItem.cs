using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using VContainer;

[RequireComponent(typeof(Collider))]
public sealed class TwoHandItemAutoRight  : XRBaseInteractable
{
    [Header("Anchors")]
    [SerializeField] private Transform beltPoint;
    [SerializeField] private Transform leftHandPoint;
    [SerializeField] private Transform rightHandPoint;

    [Header("Right-hand model")]
    [SerializeField] private Transform rightModel;

#if ENABLE_INPUT_SYSTEM
    [Header("Input")]
    [SerializeField] private InputActionReference leftSelect;
#endif

    [Header("BoolStateHub keys")]
    [Tooltip("Ключ для состояния: предмет на поясе (true/false)")]
    [SerializeField] private string keyOnBelt   = "Item.MyTool.OnBelt";
    [Tooltip("Ключ для состояния: предмет в левой руке (true/false)")]
    [SerializeField] private string keyInLeft   = "Item.MyTool.InLeftHand";

    private bool _heldLeft;

    [Inject] private BoolStateHub _stateHub;

    protected override void OnEnable()
    {
        base.OnEnable(); // <-- регистрирует интерактабл в XRInteractionManager

#if ENABLE_INPUT_SYSTEM
        if (leftSelect && leftSelect.action != null)
        {
            leftSelect.action.performed += OnLeftPress;
            leftSelect.action.canceled  += OnLeftRelease;
            if (!leftSelect.action.enabled) leftSelect.action.Enable();
        }
#endif
        if (rightModel) rightModel.gameObject.SetActive(false);
    }

    protected override void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (leftSelect && leftSelect.action != null)
        {
            leftSelect.action.performed -= OnLeftPress;
            leftSelect.action.canceled  -= OnLeftRelease;
        }
#endif
        base.OnDisable();
    }

#if ENABLE_INPUT_SYSTEM
    private void OnLeftPress(InputAction.CallbackContext _)
    {
        if (_heldLeft) return;
        if (!isHovered) return;
        if (!leftHandPoint) return;

        Snap(transform, leftHandPoint);
        _heldLeft = true;

        if (rightModel && rightHandPoint)
        {
            rightModel.gameObject.SetActive(true);
            Snap(rightModel, rightHandPoint);
        }

        SetStateOnBelt(false);
        SetStateLeft(true);
    }

    private void OnLeftRelease(InputAction.CallbackContext _)
    {
        if (!_heldLeft) return;

        if (rightModel)
        {
            rightModel.SetParent(transform, false);
            rightModel.localPosition = Vector3.zero;
            rightModel.localRotation = Quaternion.identity;
            rightModel.gameObject.SetActive(false);
        }

        if (beltPoint) Snap(transform, beltPoint);
        _heldLeft = false;

        SetStateLeft(false);
        SetStateOnBelt(true);
    }
#endif

    private static void Snap(Transform what, Transform where)
    {
        if (!what || !where) return;
        what.SetParent(where, false);
        what.localPosition = Vector3.zero;
        what.localRotation = Quaternion.identity;
    }

    private void SetStateOnBelt(bool v)  { if (_stateHub == null) return; if (v) _stateHub.SetTrue(keyOnBelt);  else _stateHub.SetFalse(keyOnBelt); }
    private void SetStateLeft(bool v)    { if (_stateHub == null) return; if (v) _stateHub.SetTrue(keyInLeft);  else _stateHub.SetFalse(keyInLeft); }
}
