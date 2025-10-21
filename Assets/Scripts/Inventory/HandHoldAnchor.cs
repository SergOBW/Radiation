    using UnityEngine;

public enum HandType { Left, Right }

public sealed class HandHoldAnchor : MonoBehaviour
{
    [SerializeField] private HandType handType = HandType.Left;
    [SerializeField] private Transform holdPoint; // пустышка на руке (позиция хвата)

    public HandType HandType => handType;
    public Transform HoldPoint => holdPoint != null ? holdPoint : transform;
}
