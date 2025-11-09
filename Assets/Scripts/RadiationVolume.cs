using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Collider))]
public class RadiationVolume : MonoBehaviour
{
    public enum Waveform { None = 0, PingPong = 1, Sine = 2 }
    public enum SpatialMode { Uniform = 0, CenterToBoundary = 1 }

    [Header("Спектр")]
    public RadiationChannel channel = RadiationChannel.Gamma;

    [Header("Пространственный режим")]
    [Tooltip("Uniform — одинаково внутри объёма. CenterToBoundary — градиент от центра к стенам/полу/потолку (для BoxCollider).")]
    public SpatialMode spatialMode = SpatialMode.CenterToBoundary;

    [Header("Uniform (если SpatialMode = Uniform)")]
    [Tooltip("Мощность дозы (µSv/h), если выбран Uniform")]
    public float doseRateInside = 100f;

    [Header("Center→Boundary (если SpatialMode = CenterToBoundary)")]
    [Tooltip("Доза в геометрическом центре комнаты (µSv/h)")]
    public float centerDose = 120f;

    [Tooltip("Доза у ближайшей стены/пола/потолка (µSv/h)")]
    public float boundaryDose = 60f;

    [Tooltip("Кривая спада от центра к границе. 1 — линейно; >1 — резче у стен; <1 — резче в центре.")]
    [Range(0.1f, 8f)]
    public float falloffPower = 1f;

    [Header("Анимация по времени (опционально)")]
    public Waveform waveform = Waveform.None;

    [Tooltip("Период полного цикла анимации, сек")]
    public float periodSeconds = 6f;

    [Tooltip("Фазовый сдвиг, сек (для несинхронности нескольких комнат)")]
    public float phaseSeconds = 0f;

    [Tooltip("Амплитуда колебаний, ±% от базовой пространственной дозы")]
    [Range(0f, 200f)]
    public float animAmplitudePercent = 10f;

    private Collider _col;

    private void Awake()
    {
        _col = GetComponent<Collider>();
        _col.isTrigger = true;
        Validate();
    }

    private void OnValidate()
    {
        if (_col == null) _col = GetComponent<Collider>();
        if (_col != null) _col.isTrigger = true;
        Validate();
    }

    public bool Contains(Vector3 worldPos)
    {
        return _col != null && _col.bounds.Contains(worldPos);
    }

    public float GetDoseRate(Vector3 worldPos)
    {
        if (!Contains(worldPos))
            return 0f;

        float baseSpatialDose = ComputeSpatialDose(worldPos);
        float animated = ApplyTemporalAnimation(baseSpatialDose);
        if (animated < 0f) animated = 0f;
        return animated;
    }

    private float ComputeSpatialDose(Vector3 worldPos)
    {
        if (spatialMode == SpatialMode.Uniform)
            return doseRateInside;

        // Поддержка градиента нормальная — для BoxCollider
        BoxCollider box = _col as BoxCollider;
        if (box == null)
        {
            // Фолбэк: если не BoxCollider — ведём себя как Uniform с centerDose
            return centerDose;
        }

        // Локальная позиция относительно центра коллайдера
        Vector3 local = transform.InverseTransformPoint(worldPos) - box.center;

        // Полуразмеры (в локальном пространстве)
        Vector3 half = new Vector3(
            Mathf.Abs(box.size.x) * 0.5f,
            Mathf.Abs(box.size.y) * 0.5f,
            Mathf.Abs(box.size.z) * 0.5f);

        // Расстояния до ближайших граней по осям
        float dx = half.x - Mathf.Abs(local.x);
        float dy = half.y - Mathf.Abs(local.y);
        float dz = half.z - Mathf.Abs(local.z);

        // Минимальная дистанция до любой грани (0 — у стены/пола/потолка; максимум — в центре)
        float minDistToFace = Mathf.Min(dx, Mathf.Min(dy, dz));
        if (minDistToFace < 0f) minDistToFace = 0f;

        // Дистанция в центре — это минимальный из полуразмеров
        float centerMinDist = Mathf.Min(half.x, Mathf.Min(half.y, half.z));
        if (centerMinDist < 0.0001f) centerMinDist = 0.0001f;

        // Нормированная близость к границе: 0 в центре, 1 у стен/пола/потолка
        float s = 1f - Mathf.Clamp01(minDistToFace / centerMinDist);

        // Нелинейная кривая
        if (falloffPower != 1f)
            s = Mathf.Pow(s, falloffPower);

        // Лерп от centerDose (s=0) к boundaryDose (s=1)
        float minV = Mathf.Min(centerDose, boundaryDose);
        float maxV = Mathf.Max(centerDose, boundaryDose);
        float result = Mathf.Lerp(centerDose, boundaryDose, s);

        // На всякий случай — не ниже 0
        if (result < 0f) result = 0f;
        return result;
    }

    private float ApplyTemporalAnimation(float baseDose)
    {
        if (waveform == Waveform.None) return baseDose;
        if (periodSeconds <= 0.0001f || animAmplitudePercent <= 0f) return baseDose;

        float t = (Time.time + phaseSeconds) / periodSeconds;
        float oscill = 0f; // -1..+1

        if (waveform == Waveform.PingPong)
        {
            // Преобразуем PingPong 0..1 в -1..+1 (треугольник)
            float tri = Mathf.PingPong(t * 2f, 1f);      // 0..1
            oscill = (tri - 0.5f) * 2f;                  // -1..+1
        }
        else if (waveform == Waveform.Sine)
        {
            float angle = t * Mathf.PI * 2f;
            oscill = Mathf.Sin(angle);                   // -1..+1
        }

        float factor = 1f + (animAmplitudePercent * 0.01f) * oscill;
        float animated = baseDose * factor;
        return animated;
    }

    private void OnDrawGizmosSelected()
    {
        if (_col == null) _col = GetComponent<Collider>();

        Gizmos.color = new Color(
            (channel & RadiationChannel.Gamma) != 0 ? 0.2f : 0.05f,
            (channel & RadiationChannel.Beta)  != 0 ? 1f   : 0.2f,
            0.2f, 0.2f);

        Gizmos.matrix = transform.localToWorldMatrix;

        BoxCollider b = _col as BoxCollider;
        if (b != null)
            Gizmos.DrawWireCube(b.center, b.size);
        else
            Gizmos.DrawWireSphere(Vector3.zero, 1f);
    }

    private void Validate()
    {
        if (doseRateInside < 0f) doseRateInside = 0f;
        if (centerDose < 0f) centerDose = 0f;
        if (boundaryDose < 0f) boundaryDose = 0f;
        if (periodSeconds < 0.01f) periodSeconds = 0.01f;
        if (falloffPower < 0.1f) falloffPower = 0.1f;
    }
}
