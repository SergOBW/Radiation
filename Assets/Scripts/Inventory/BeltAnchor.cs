using UnityEngine;

public sealed class BeltAnchor : MonoBehaviour
{
    [SerializeField] private Transform anchor; // пустышка на поясе (позиция хранения)
    public Transform Anchor => anchor != null ? anchor : transform;
}