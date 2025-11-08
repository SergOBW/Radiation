using System.Collections.Generic;
using UnityEngine;

public sealed class ControllerVisibilityByHold : MonoBehaviour
{
    [Header("Controller visuals to toggle")]
    [SerializeField] private List<GameObject> leftControllerVisuals = new();
    [SerializeField] private List<GameObject> rightControllerVisuals = new();

    [SerializeField] private List<Behaviour> leftControllerBehaviours = new();
    [SerializeField] private List<Behaviour> rightControllerBehaviours = new();

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
        {
            SetVisible(leftControllerVisuals, visible);
            SetActive(leftControllerBehaviours, visible);
        }
        else
        {
            SetVisible(rightControllerVisuals, visible);
            SetActive(rightControllerBehaviours, visible);
        }
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

    private static void SetActive(List<Behaviour> list, bool enable)
    {
        if (list == null) return;

        foreach (var behaviour in list)
        {
            behaviour.enabled = enable;
        }

    }
}