using System.Collections.Generic;
using UnityEngine;

public sealed class ControllerVisibilityByHold : MonoBehaviour
{
    [Header("Controller visuals to toggle")]
    [SerializeField] private List<GameObject> leftControllerVisuals = new();
    [SerializeField] private List<GameObject> rightControllerVisuals = new();

    private void Start()
    {
        if (HoldStateBus.Instance == null)
            return;

        HoldStateBus.Instance.HoldCountChanged += OnHoldChanged;

        SetVisible(leftControllerVisuals,  !HoldStateBus.Instance.IsHeld(HandSide.Left));
        SetVisible(rightControllerVisuals, !HoldStateBus.Instance.IsHeld(HandSide.Right));
    }

    private void OnDisable()
    {
        if (HoldStateBus.Instance != null)
            HoldStateBus.Instance.HoldCountChanged -= OnHoldChanged;
    }

    private void OnHoldChanged(HandSide side, int count)
    {
        bool visible = count == 0;
        if (side == HandSide.Left)
            SetVisible(leftControllerVisuals, visible);
        else
            SetVisible(rightControllerVisuals, visible);
    }

    private static void SetVisible(List<GameObject> list, bool visible)
    {
        if (list == null) return;

        foreach (var go in list)
        {
            if (go && go.activeSelf != visible)
                go.SetActive(visible);
        }
    }
}