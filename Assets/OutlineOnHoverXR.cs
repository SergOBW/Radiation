using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// В VR включает Outline на наведении и выключает при уходе курсора/руки.
/// Требует на том же объекте Outline и XRBaseInteractable (например, XRGrabInteractable).
/// </summary>
[RequireComponent(typeof(Outline))]
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable))]
public sealed class OutlineOnHoverXR : MonoBehaviour
{
    [Header("Outline во время захвата")]
    [SerializeField] private bool enableWhileSelected = true;

    [Header("Параметры обводки")]
    [SerializeField] private Outline.Mode outlineMode = Outline.Mode.OutlineVisible;
    [SerializeField] private Color hoverColor = Color.cyan;
    [SerializeField, Range(0f, 10f)] private float hoverWidth = 4f;

    private Outline _outline;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable _interactable;
    private int _hoverCount;

    private void Awake()
    {
        _outline = GetComponent<Outline>();
        _interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();

        // На старте обводка выключена (Outline сам добавляет/убирает материалы в OnEnable/OnDisable)
        if (_outline.enabled)
        {
            _outline.enabled = false;
        }
    }

    private void OnEnable()
    {
        _interactable.hoverEntered.AddListener(OnHoverEntered);
        _interactable.hoverExited.AddListener(OnHoverExited);
        _interactable.selectEntered.AddListener(OnSelectEntered);
        _interactable.selectExited.AddListener(OnSelectExited);
    }

    private void OnDisable()
    {
        _interactable.hoverEntered.RemoveListener(OnHoverEntered);
        _interactable.hoverExited.RemoveListener(OnHoverExited);
        _interactable.selectEntered.RemoveListener(OnSelectEntered);
        _interactable.selectExited.RemoveListener(OnSelectExited);

        _hoverCount = 0;
        DisableOutline();
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        _hoverCount += 1;
        ApplyOutlineSettings();
        EnableOutline();
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        if (_hoverCount > 0)
        {
            _hoverCount -= 1;
        }

        if (_hoverCount == 0 && (!enableWhileSelected || !_interactable.isSelected))
        {
            DisableOutline();
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (enableWhileSelected)
        {
            ApplyOutlineSettings();
            EnableOutline();
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (_hoverCount == 0)
        {
            DisableOutline();
        }
    }

    private void ApplyOutlineSettings()
    {
        _outline.OutlineMode = outlineMode;
        _outline.OutlineColor = hoverColor;
        _outline.OutlineWidth = hoverWidth;
    }

    private void EnableOutline()
    {
        if (!_outline.enabled)
        {
            _outline.enabled = true;
        }
    }

    private void DisableOutline()
    {
        if (_outline.enabled)
        {
            _outline.enabled = false;
        }
    }
}
