using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RadiationSampleZone : MonoBehaviour
{
    [Header("Идентификатор/название зоны")]
    public string pointName = "Зона без имени";

    [Header("Источник измерений")]
    public DosimeterSensor sensor;

    [Header("Коллайдеры зоны")]
    [Tooltip("Если пусто и включено Auto, соберёт все Collider(ы) на этом объекте и детях")]
    public bool autoCollectCollidersInChildren = true;
    public List<Collider> zoneColliders = new();

    [Header("Окно замера")]
    public float measurementTime = 2.0f;
    [Tooltip("Допуск выхода/входа (м): если близко к границе, не сбрасывать прогресс")]
    public float edgeGrace = 0.05f;

    [Header("После замера")]
    public bool deactivateOnComplete = true;

    public event Action<RadiationSampleZone, float> Measured; // (эта зона, среднее µSv/h)

    public bool IsCompleted { get; private set; }
    public float Progress01 => Mathf.Clamp01(_accumTime / Mathf.Max(0.0001f, measurementTime));

    private bool  _measuring;
    private float _accumDose;   // µSv/h * сек
    private float _accumTime;   // сек

    private void Awake()
    {
        if (autoCollectCollidersInChildren && zoneColliders.Count == 0)
        {
            GetComponentsInChildren(true, s_buffer);
            foreach (var c in s_buffer)
                if (c != null && c.enabled) zoneColliders.Add(c);
            s_buffer.Clear();
        }
    }

    private void Update()
    {
        if (IsCompleted || sensor == null || sensor.probePoint == null || !sensor.IsWorking) return;

        var probePos = sensor.probePoint.position;
        bool inside = IsInsideAnyCollider(probePos, edgeGrace);

        if (inside)
        {
            _measuring = true;
            float dt = Time.deltaTime;
            float value = Mathf.Max(0f, sensor.CurrentDoseRateMicroSvPerHour);
            _accumDose += value * dt;
            _accumTime += dt;

            if (_accumTime >= measurementTime)
            {
                float avg = _accumDose / Mathf.Max(_accumTime, 0.0001f);
                Complete(avg);
            }
        }
        else if (_measuring)
        {
            // вышли за пределы зоны — сброс
            _measuring = false;
            _accumDose = 0f;
            _accumTime = 0f;
        }
    }

    private void Complete(float avg)
    {
        if (IsCompleted) return;
        IsCompleted = true;
        Measured?.Invoke(this, avg);
        if (deactivateOnComplete) gameObject.SetActive(false);
    }

    private bool IsInsideAnyCollider(Vector3 worldPos, float grace)
    {
        if (zoneColliders == null || zoneColliders.Count == 0) return false;

        const float eps = 1e-5f;
        float graceSqr = Mathf.Max(0f, grace) * Mathf.Max(0f, grace);

        foreach (var col in zoneColliders)
        {
            if (col == null || !col.enabled) continue;

            // Быстрое отсечение по Bounds (с учётом grace)
            var b = col.bounds;
            b.Expand(grace * 2f);
            if (!b.Contains(worldPos)) continue;

            // Проверяем через ClosestPoint: если внутри, ClosestPoint == точка (или очень близко)
            Vector3 cp = col.ClosestPoint(worldPos);
            float sq = (cp - worldPos).sqrMagnitude;
            if (sq <= eps || sq <= graceSqr)
                return true;
        }
        return false;
    }

    // для быстрого сбора
    private static readonly List<Collider> s_buffer = new();
}
