using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Скрипт для кейса, который можно взять и открыть/закрыть в VR.
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public sealed class CaseInteractable : MonoBehaviour
{
    [Header("Крышка кейса")]
    [SerializeField] private Transform lid; // Объект крышки
    [SerializeField] private Vector3 closedRotation = Vector3.zero;
    [SerializeField] private Vector3 openedRotation = new Vector3(-120f, 0f, 0f);
    [SerializeField] private float openSpeed = 5f;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grab;
    private bool _isOpen;
    private bool _isHeld;
    private Transform _interactorTransform;

    private void Awake()
    {
        _grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        _grab.selectEntered.AddListener(OnGrab);
        _grab.selectExited.AddListener(OnRelease);
    }

    private void OnDestroy()
    {
        _grab.selectEntered.RemoveListener(OnGrab);
        _grab.selectExited.RemoveListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        _isHeld = true;
        _interactorTransform = args.interactorObject.transform;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        _isHeld = false;
        _interactorTransform = null;
    }

    private void Update()
    {
        if (!_isHeld || _interactorTransform == null)
            return;

        // Считываем триггер с XR контроллера
        if (TryGetTriggerPressed(_interactorTransform))
        {
            ToggleCase();
        }

        // Анимация открытия/закрытия
        Quaternion targetRot = Quaternion.Euler(_isOpen ? openedRotation : closedRotation);
        lid.localRotation = Quaternion.Slerp(lid.localRotation, targetRot, Time.deltaTime * openSpeed);
    }

    private void ToggleCase()
    {
        _isOpen = !_isOpen;
        Debug.Log($"[Case] Состояние: {(_isOpen ? "Открыт" : "Закрыт")}");
    }

    /// <summary>
    /// Проверка нажатия триггера (работает для OpenXR / XR Input).
    /// </summary>
    private bool TryGetTriggerPressed(Transform interactor)
    {
        if (interactor.TryGetComponent(out ActionBasedController controller))
        {
            // Нажатие основного триггера (значение от 0 до 1)
            return controller.activateAction.action.ReadValue<float>() > 0.9f;
        }
        return false;
    }
}
