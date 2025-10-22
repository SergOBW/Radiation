using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Включает Outline при наведении/захвате XR, поддерживает "виртуальное выделение".
/// Можно сослаться на XRBaseInteractable на другом объекте через инспектор.
/// Если ссылки нет и на этом объекте тоже нет XRBaseInteractable — компонент сам выключится.
/// </summary>
[RequireComponent(typeof(Outline))]
public sealed class OutlineOnHoverXR : MonoBehaviour
{
    [Header("Источник XRBaseInteractable (опционально чужой объект)")]
    [SerializeField] private XRBaseInteractable interactable; // можно указать в инспекторе
    [SerializeField] private bool tryFindOnThisObjectIfNull = true;

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
    private int _hoverCount;
    private bool _virtuallySelected;
    private bool _subscribed;

    private void Awake()
    {
        _outline = GetComponent<Outline>();

        if (interactable == null && tryFindOnThisObjectIfNull)
            TryGetComponent(out interactable);

        if (_outline.enabled) _outline.enabled = false;

        // Если так и не нашли XRBaseInteractable — безопасно отключаемся
        if (interactable == null)
        {
            Debug.LogWarning($"[{nameof(OutlineOnHoverXR)}] XRBaseInteractable не найден ни в поле, ни на этом объекте. Компонент будет отключен.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (interactable == null) return;
        Subscribe();
        Reevaluate();
    }

    private void OnDisable()
    {
        Unsubscribe();
        _hoverCount = 0;
        DisableOutline();
    }

    // Доступ извне
    public void SetVirtualSelected(bool value)
    {
        _virtuallySelected = value;
        Reevaluate();
    }

    public bool IsVirtualSelected() => _virtuallySelected;

    // Подписки
    private void Subscribe()
    {
        if (_subscribed || interactable == null) return;

        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);
        interactable.selectEntered.AddListener(OnSelectEntered);
        interactable.selectExited.AddListener(OnSelectExited);

        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || interactable == null) return;

        interactable.hoverEntered.RemoveListener(OnHoverEntered);
        interactable.hoverExited.RemoveListener(OnHoverExited);
        interactable.selectEntered.RemoveListener(OnSelectEntered);
        interactable.selectExited.RemoveListener(OnSelectExited);

        _subscribed = false;
    }

    // XR callbacks
    private void OnHoverEntered(HoverEnterEventArgs _) { _hoverCount++; Reevaluate(); }
    private void OnHoverExited(HoverExitEventArgs _)   { if (_hoverCount > 0) _hoverCount--; Reevaluate(); }
    private void OnSelectEntered(SelectEnterEventArgs _) { Reevaluate(); }
    private void OnSelectExited(SelectExitEventArgs _)   { Reevaluate(); }

    // Логика отображения
    private void Reevaluate()
    {
        if (interactable == null) { DisableOutline(); return; }

        bool hovered = _hoverCount > 0;
        bool xrSelected = interactable.isSelected;

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

#if UNITY_EDITOR
    // Удобство в инспекторе — авто-подстановка при изменениях
    private void OnValidate()
    {
        if (interactable == null && tryFindOnThisObjectIfNull)
            TryGetComponent(out interactable);
    }
#endif
}
