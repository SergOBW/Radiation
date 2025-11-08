using UnityEngine;

public sealed class FloatingArrow : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] private float amplitude = 0.2f;
    [SerializeField] private float speed = 2f;

    private Vector3 _startPosition;

    private void Start()
    {
        _startPosition = transform.localPosition;
    }

    private void Update()
    {
        float offset = Mathf.Sin(Time.time * speed) * amplitude;
        transform.localPosition = _startPosition + new Vector3(0f, 0f, offset);
    }
}

