using UnityEngine;

public sealed class ControllerVisibilityByHold : MonoBehaviour
{
    [Header("Controller visuals")]
    [SerializeField] private GameObject leftControllerVisual;
    [SerializeField] private GameObject rightControllerVisual;

    private void Start()
    {
        if (HoldStateBus.Instance != null)
        {
            HoldStateBus.Instance.HoldCountChanged += OnHoldChanged;

            // Синхронизируемся с текущим состоянием (на случай, если включились позже)
            SetVisible(leftControllerVisual,  !HoldStateBus.Instance.IsHeld(HandSide.Left));
            SetVisible(rightControllerVisual, !HoldStateBus.Instance.IsHeld(HandSide.Right));
        }
    }

    private void OnDisable()
    {
        if (HoldStateBus.Instance != null)
            HoldStateBus.Instance.HoldCountChanged -= OnHoldChanged;
    }

    private void OnHoldChanged(HandSide side, int count)
    {
        if (side == HandSide.Left)
            SetVisible(leftControllerVisual,  count == 0);
        else
            SetVisible(rightControllerVisual, count == 0);
    }

    private static void SetVisible(GameObject go, bool visible)
    {
        if (go && go.activeSelf != visible) go.SetActive(visible);
    }
}