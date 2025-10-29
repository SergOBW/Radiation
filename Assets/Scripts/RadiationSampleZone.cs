using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RadiationSampleZone : MonoBehaviour
{
    [Header("Идентификатор/название зоны")]
    public string pointName = "Зона без имени";

    [Header("Каналы для измерения")]
    public RadiationChannel measureChannels = RadiationChannel.Gamma;

    [Header("Коллайдеры зоны")]
    [Tooltip("Если пусто и включено Auto, соберёт все Collider(ы) на этом объекте и детях")]
    public bool autoCollectCollidersInChildren = true;
    public List<Collider> zoneColliders = new();

    [Header("Окно замера")]
    public float measurementTime = 2.0f;

    [Header("После замера")]
    public bool deactivateOnComplete = true;

    public bool IsCompleted { get; set; }

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

    public void CompleteAndDeactivate()
    {
        if (IsCompleted) return;
        IsCompleted = true;
        if (deactivateOnComplete)
            gameObject.SetActive(false);
    }

    public bool IsInsideAnyCollider(Vector3 worldPos, float grace)
    {
        if (zoneColliders == null || zoneColliders.Count == 0) return false;

        const float eps = 1e-5f;
        float graceSqr = Mathf.Max(0f, grace) * Mathf.Max(0f, grace);

        foreach (var col in zoneColliders)
        {
            if (col == null || !col.enabled) continue;

            var b = col.bounds;
            b.Expand(grace * 2f);
            if (!b.Contains(worldPos)) continue;

            Vector3 cp = col.ClosestPoint(worldPos);
            float sq = (cp - worldPos).sqrMagnitude;
            if (sq <= eps || sq <= graceSqr)
                return true;
        }
        return false;
    }

    private static readonly List<Collider> s_buffer = new();
}
