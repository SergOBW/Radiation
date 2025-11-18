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
    public enum CombineMode
    {
        Additive = 0,
        Max = 1,
        SoftCap = 2,
        Nearest = 3,
        TopKMean = 4
    }

    public bool IsWorking = true;

    [Header("Чувствительность сенсора")]
    public RadiationChannel sensitivity = RadiationChannel.Gamma;

    [Header("Точка замера (кончик зонда)")]
    public Transform probePoint;

    [Header("Сэмплинг")]
    [Tooltip("Гц — сколько раз в секунду обновляем измерение")]
    public float sampleRate = 10f;
    [Tooltip("Плавное сглаживание показаний (сек)")]
    public float smoothingTime = 0.5f;
    [Tooltip("Максимальная дальность поиска источников")]
    public float maxSearchDistance = 50f;

    [Header("Комбинирование вкладов источников")]
    public CombineMode combineMode = CombineMode.SoftCap;

    [Tooltip("Порог для SoftCap (µSv/h). Формула: cap * (1 - exp(-sum/cap))")]
    public float softCap = 200f;

    [Tooltip("Для TopKMean: сколько сильнейших учитываем")]
    public int topK = 2;

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

        CurrentDoseRateMicroSvPerHour = Mathf.SmoothDamp(
            CurrentDoseRateMicroSvPerHour, _target, ref _vel, smoothingTime);
    }

    private float SampleDoseRate(Vector3 pos)
    {
        const int MaxContrib = 256;
        float[] contribs = new float[MaxContrib];
        int count = 0;

        RadiationSource[] sources = FindObjectsByType<RadiationSource>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        RadiationVolume[] volumes = FindObjectsByType<RadiationVolume>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        int i;

        for (i = 0; i < sources.Length && count < MaxContrib; i++)
        {
            RadiationSource s = sources[i];
            if (!s.isActiveAndEnabled) continue;
            if ((s.channel & sensitivity) == 0) continue;
            if (Vector3.Distance(pos, s.transform.position) > maxSearchDistance) continue;

            float v = s.GetDoseRate(pos);
            if (v > 0f)
            {
                contribs[count] = v;
                count++;
            }
        }

        for (i = 0; i < volumes.Length && count < MaxContrib; i++)
        {
            RadiationVolume v = volumes[i];
            if (!v.isActiveAndEnabled) continue;
            if ((v.channel & sensitivity) == 0) continue;
            float c = v.GetDoseRate(pos);
            if (c > 0f)
            {
                contribs[count] = c;
                count++;
            }
        }

        if (count == 0) return 0f;

        if (combineMode == CombineMode.Additive)
        {
            float sum = 0f;
            for (i = 0; i < count; i++) sum += contribs[i];
            return Mathf.Max(0f, sum);
        }
        else if (combineMode == CombineMode.Max || combineMode == CombineMode.Nearest)
        {
            float max = contribs[0];
            for (i = 1; i < count; i++) if (contribs[i] > max) max = contribs[i];
            return Mathf.Max(0f, max);
        }
        else if (combineMode == CombineMode.SoftCap)
        {
            float sum = 0f;
            for (i = 0; i < count; i++) sum += contribs[i];
            float cap = Mathf.Max(0.0001f, softCap);
            float saturated = cap * (1f - Mathf.Exp(-sum / cap));
            return Mathf.Max(0f, saturated);
        }
        else
        {
            int k = Mathf.Clamp(topK, 1, count);

            for (int a = 0; a < count - 1; a++)
            {
                int maxIdx = a;
                for (int b = a + 1; b < count; b++)
                {
                    if (contribs[b] > contribs[maxIdx]) maxIdx = b;
                }
                if (maxIdx != a)
                {
                    float tmp = contribs[a];
                    contribs[a] = contribs[maxIdx];
                    contribs[maxIdx] = tmp;
                }
            }

            float sumTop = 0f;
            for (i = 0; i < k; i++) sumTop += contribs[i];
            float mean = sumTop / k;
            return Mathf.Max(0f, mean);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (probePoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(probePoint.position, 0.05f);
        }
    }
}
