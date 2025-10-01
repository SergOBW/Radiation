using UnityEngine;

public enum FalloffMode { InverseSquare, Linear, None }

[ExecuteAlways]
public class RadiationSource : MonoBehaviour
{
    [Header("Интенсивность")]
    [Tooltip("Мощность дозы на расстоянии 1 метр (µSv/h)")]
    public float doseRateAt1m = 50f;

    [Header("Затухание")]
    public FalloffMode falloff = FalloffMode.InverseSquare;
    [Tooltip("Радиус влияния (для Linear/None). Для InverseSquare можно оставить ≈ максимальной дистанции обнаружения.")]
    public float radius = 20f;
    [Tooltip("Минимальная дистанция (смягчение сингулярности в 0)")]
    public float minDistance = 0.2f;

    [Header("Шум")]
    [Tooltip("Случайные пульсации для реалистичности (доля от значения)")]
    [Range(0f, 0.5f)] public float noiseFraction = 0.05f;

    [Header("Экранирование (опционально)")]
    public bool checkObstruction = false;
    public LayerMask obstructionMask;
    [Tooltip("Коэффициент ослабления при частичном экранировании (0..1). 0 = полностью гасим")]
    [Range(0f,1f)] public float obstructionAttenuation = 0.2f;

    public float GetDoseRate(Vector3 worldPos)
    {
        float baseRate = 0f;
        float dist = Mathf.Max(Vector3.Distance(worldPos, transform.position), minDistance);

        switch (falloff)
        {
            case FalloffMode.InverseSquare:
                baseRate = doseRateAt1m / (dist * dist);
                break;
            case FalloffMode.Linear:
                if (dist > radius) baseRate = 0f;
                else baseRate = doseRateAt1m * Mathf.Lerp(1f, 0f, dist / Mathf.Max(radius, 0.001f));
                break;
            case FalloffMode.None:
                baseRate = (dist <= radius) ? doseRateAt1m : 0f;
                break;
        }

        if (checkObstruction)
        {
            Vector3 dir = (transform.position - worldPos).normalized;
            float d = Vector3.Distance(worldPos, transform.position);
            if (Physics.Raycast(worldPos, dir, d, obstructionMask, QueryTriggerInteraction.Ignore))
                baseRate *= obstructionAttenuation;
        }

        if (noiseFraction > 0f && Application.isPlaying)
        {
            float n = (Mathf.PerlinNoise(Time.time * 2.31f + transform.position.x, Time.time * 1.73f + transform.position.z) - 0.5f) * 2f;
            baseRate *= (1f + n * noiseFraction);
        }

        return Mathf.Max(0f, baseRate);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(radius, 0.1f));
    }
}
