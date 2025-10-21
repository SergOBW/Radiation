using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// В VR включает Outline на наведении и/или во время XR-захвата,
/// а также умеет показывать стиль "виртуального выделения" (нажатие по триггеру при наведении).
/// Требует Outline и XRBaseInteractable.
/// </summary>
[RequireComponent(typeof(Outline))]
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable))]
public sealed class OutlineOnHoverXR : MonoBehaviour
{
    [Header("Включать Outline, пока объект захвачен (XR select)")]
    [SerializeField] private bool enableWhileSelected = true;

    [Header("Ховер-стиль")]
    [SerializeField] private Outline.Mode hoverMode = Outline.Mode.OutlineVisible;
    [SerializeField] private Color hoverColor = Color.cyan;
    [SerializeField, Range(0f, 10f)] private float hoverWidth = 4f;

    [Header("Стиль ВИРТУАЛЬНОГО ВЫДЕЛЕНИЯ")]
    [SerializeField] private bool useSelectedStyle = true;
    [SerializeField] private Outline.Mode selectedMode = Outline.Mode.OutlineVisible;
    [SerializeField] private Color selectedColor = new Color(0.4f, 1f, 0.6f, 1f);
    [SerializeField, Range(0f, 10f)] private float selectedWidth = 6f;

    private Outline _outline;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable _interactable;

    private int _hoverCount;
    private bool _virtuallySelected; // наше "выделение по триггеру при наведении"

    private void Awake()
    {
        _outline = GetComponent<Outline>();
        _interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();

        if (_outline.enabled) _outline.enabled = false;
    }

    private void OnEnable()
    {
        _interactable.hoverEntered.AddListener(OnHoverEntered);
        _interactable.hoverExited.AddListener(OnHoverExited);
        _interactable.selectEntered.AddListener(OnSelectEntered);
        _interactable.selectExited.AddListener(OnSelectExited);

        Reevaluate();
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

    // === Публичный API для внешних скриптов (наш селектор будет сюда дергать) ===
    public void SetVirtualSelected(bool value)
    {
        _virtuallySelected = value;
        Reevaluate();
    }

    public bool IsVirtualSelected() => _virtuallySelected;

    // === XR callbacks ===
    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        _hoverCount += 1;
        Reevaluate();
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        if (_hoverCount > 0) _hoverCount -= 1;
        Reevaluate();
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        Reevaluate();
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        Reevaluate();
    }

    // === Логика выбора подходящего стиля и вкл/выкл ===
    private void Reevaluate()
    {
        bool hovered = _hoverCount > 0;
        bool xrSelected = _interactable.isSelected;

        if (_virtuallySelected && useSelectedStyle)
        {
            ApplySelectedStyle();
            EnableOutline();
            return;
        }

        if (hovered || (enableWhileSelected && xrSelected))
        {
            ApplyHoverStyle();
            EnableOutline();
            return;
        }

        DisableOutline();
    }

    private void ApplyHoverStyle()
    {
        _outline.OutlineMode = hoverMode;
        _outline.OutlineColor = hoverColor;
        _outline.OutlineWidth = hoverWidth;
    }

    private void ApplySelectedStyle()
    {
        _outline.OutlineMode = selectedMode;
        _outline.OutlineColor = selectedColor;
        _outline.OutlineWidth = selectedWidth;
    }

    private void EnableOutline()
    {
        if (!_outline.enabled) _outline.enabled = true;
    }

    private void DisableOutline()
    {
        if (_outline.enabled) _outline.enabled = false;
    }
}
