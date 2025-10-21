using System;
using UnityEngine;

public enum HandSide { Left, Right }

public sealed class HoldStateBus : MonoBehaviour
{
    public static HoldStateBus Instance { get; private set; }

    public event Action<HandSide,int> HoldCountChanged;

    [SerializeField] private int leftCount;
    [SerializeField] private int rightCount;

    public bool IsHeld(HandSide side) => side == HandSide.Left ? leftCount > 0 : rightCount > 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void BeginHold(HandSide side)
    {
        if (side == HandSide.Left)  { leftCount  = Mathf.Max(0, leftCount)  + 1; HoldCountChanged?.Invoke(HandSide.Left,  leftCount); }
        else                        { rightCount = Mathf.Max(0, rightCount) + 1; HoldCountChanged?.Invoke(HandSide.Right, rightCount); }
    }

    public void EndHold(HandSide side)
    {
        if (side == HandSide.Left)  { leftCount  = Mathf.Max(0, leftCount  - 1); HoldCountChanged?.Invoke(HandSide.Left,  leftCount); }
        else                        { rightCount = Mathf.Max(0, rightCount - 1); HoldCountChanged?.Invoke(HandSide.Right, rightCount); }
    }
}