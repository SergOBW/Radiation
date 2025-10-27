using System.Linq;
using UnityEngine;
[System.Flags]
public enum RadiationChannel
{
    None  = 0,
    Gamma = 1 << 0,
    Beta  = 1 << 1,
}

public class DosimeterSensor : MonoBehaviour
{
    public bool IsWorking = true;

    [Header("Чувствительность сенсора (что считаем)")]
    public RadiationChannel sensitivity = RadiationChannel.Gamma;

    [Header("Точка замера (кончик зонда/антенна)")]
    public Transform probePoint;

    [Header("Сэмплинг")]
    [Tooltip("Гц — сколько раз в секунду обновляем измерение")]
    public float sampleRate = 10f;
    [Tooltip("Плавное сглаживание показаний (сек)")]
    public float smoothingTime = 0.5f;
    [Tooltip("Максимальная дальность поиска источников")]
    public float maxSearchDistance = 50f;

    [Header("Фильтр поиска (опционально)")]
    public LayerMask sourcesQueryMask; // можно оставить Default если не используете
    public bool usePhysicsOverlapSphere = false;

    public float CurrentDoseRateMicroSvPerHour { get; private set; }

    private float _target;
    private float _vel;
    private float _nextSampleTime;

    private void Update()
    {
        if (!IsWorking) return;
        if (probePoint == null) return;

        if (Time.time >= _nextSampleTime)
        {
            _nextSampleTime = Time.time + 1f / Mathf.Max(1f, sampleRate);
            _target = SampleDoseRate(probePoint.position);
        }

        CurrentDoseRateMicroSvPerHour = Mathf.SmoothDamp(CurrentDoseRateMicroSvPerHour, _target, ref _vel, smoothingTime);
    }

    float SampleDoseRate(Vector3 pos)
    {
        float total = 0f;

        RadiationSource[] sources;
        RadiationVolume[] volumes;

        if (usePhysicsOverlapSphere)
        {
            var hits = Physics.OverlapSphere(pos, maxSearchDistance, ~0, QueryTriggerInteraction.Collide);
            sources = hits.Select(h => h.GetComponent<RadiationSource>()).Where(s => s != null).ToArray();
            volumes = hits.Select(h => h.GetComponent<RadiationVolume>()).Where(v => v != null).ToArray();
        }
        else
        {
            sources = FindObjectsByType<RadiationSource>(FindObjectsSortMode.None);
            volumes = FindObjectsByType<RadiationVolume>(FindObjectsSortMode.None);
        }

        foreach (var s in sources)
        {
            if (!s.isActiveAndEnabled) continue;
            if ((s.channel & sensitivity) == 0) continue; // <-- фильтрация по спектру
            if (Vector3.Distance(pos, s.transform.position) > maxSearchDistance) continue;

            total += s.GetDoseRate(pos);
        }

        foreach (var v in volumes)
        {
            if (!v.isActiveAndEnabled) continue;
            if ((v.channel & sensitivity) == 0) continue; // <-- фильтрация по спектру
            total += v.GetDoseRate(pos);
        }

        return Mathf.Max(0f, total);
    }

    private void OnDrawGizmosSelected()
    {
        if (probePoint)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(probePoint.position, 0.05f);
        }
    }
}
