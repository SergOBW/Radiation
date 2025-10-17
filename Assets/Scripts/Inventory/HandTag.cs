using UnityEngine;

public enum HandType
{
    Left,
    Right
}

public sealed class HandTag : MonoBehaviour
{
    [SerializeField] private HandType handType = HandType.Left;

    public HandType HandType
    {
        get { return handType; }
    }
}