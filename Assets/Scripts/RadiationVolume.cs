using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Collider))]
public class RadiationVolume : MonoBehaviour
{
    [Tooltip("Мощность дозы внутри объёма (µSv/h)")]
    public float doseRateInside = 100f;

    private Collider _col;

    private void Awake()
    {
        _col = GetComponent<Collider>();
        _col.isTrigger = true;
    }

    public bool Contains(Vector3 worldPos) => _col.bounds.Contains(worldPos);

    public float GetDoseRate(Vector3 worldPos)
    {
        return Contains(worldPos) ? doseRateInside : 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (!_col) _col = GetComponent<Collider>();
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.2f);
        Gizmos.matrix = transform.localToWorldMatrix;
        if (_col is BoxCollider b)
            Gizmos.DrawWireCube(b.center, b.size);
        else
            Gizmos.DrawWireSphere(Vector3.zero, 1f);
    }
}