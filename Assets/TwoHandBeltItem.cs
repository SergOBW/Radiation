using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Collider))]
public sealed class TwoHandItemAutoRight : XRBaseInteractable
{
    [Header("Anchors")]
    [SerializeField] private Transform beltPoint;
    [SerializeField] private Transform leftHandPoint;  // точка в левой руке (для корня)
    [SerializeField] private Transform rightHandPoint; // точка в правой руке (для правой модели)

    [Header("Right-hand model")]
    [SerializeField] private Transform rightModel;     // отдельная модель правой руки (обычно скрыта)

#if ENABLE_INPUT_SYSTEM
    [Header("Input")]
    [SerializeField] private InputActionReference leftSelect;   // performed -> взять; canceled -> вернуть
#endif

    private bool _isHovering;   // навёлся (любой интерактор — обычно левая)
    private bool _heldLeft;     // предмет в левой руке

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
        base.OnEnable();

        if (leftSelect && leftSelect.action != null)
        {
            leftSelect.action.performed += OnLeftPress;
            leftSelect.action.canceled  += OnLeftRelease;
            if (!leftSelect.action.enabled) leftSelect.action.Enable();
        }

        // правая модель по умолчанию выключена
        if (rightModel) rightModel.gameObject.SetActive(false);
    }

    protected override void OnDisable()
    {
        if (leftSelect && leftSelect.action != null)
        {
            leftSelect.action.performed -= OnLeftPress;
            leftSelect.action.canceled  -= OnLeftRelease;
        }
        base.OnDisable();
    }

    private void OnLeftPress(InputAction.CallbackContext _)
    {
        if (_heldLeft) return;
        if (!_isHovering) return;              // сначала навёлся, потом нажал
        if (!leftHandPoint) return;

        // корень → в левую руку
        Snap(transform, leftHandPoint);
        _heldLeft = true;
        HoldStateBus.Instance?.BeginHold(HandSide.Left);

        // правая модель: включить и посадить в правую руку (если настроена)
        if (rightModel && rightHandPoint)
        {
            HoldStateBus.Instance?.BeginHold(HandSide.Right);
            rightModel.gameObject.SetActive(true);
            Snap(rightModel, rightHandPoint);
        }
    }

    private void OnLeftRelease(InputAction.CallbackContext _)
    {
        if (!_heldLeft) return;

        // правая модель: спрятать и вернуть под корень
        if (rightModel)
        {
            rightModel.SetParent(transform, false);
            rightModel.localPosition = Vector3.zero;
            rightModel.localRotation = Quaternion.identity;
            rightModel.gameObject.SetActive(false);
            HoldStateBus.Instance?.EndHold(HandSide.Right);
        }

        // корень → на пояс
        if (beltPoint) Snap(transform, beltPoint);
        _heldLeft = false;
        HoldStateBus.Instance?.EndHold(HandSide.Left);
    }
#endif

    private static void Snap(Transform what, Transform where)
    {
        if (!what || !where) return;
        what.SetParent(where, false);
        what.localPosition = Vector3.zero;
        what.localRotation = Quaternion.identity;
    }
}
