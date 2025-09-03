using System.Collections.Generic;
using UnityEngine;

public sealed class WaypointRegistry
{
    private readonly Dictionary<string, Transform> _points = new Dictionary<string, Transform>();

    public void Register(string id, Transform t)
    {
        if (string.IsNullOrWhiteSpace(id) || t == null) return;
        _points[id] = t;
    }

    public void Unregister(string id, Transform t)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        if (_points.TryGetValue(id, out var cur) && cur == t) _points.Remove(id);
    }

    public bool TryGet(string id, out Transform t) => _points.TryGetValue(id, out t);
}