using System.Collections.Generic;
using UnityEngine;

public sealed class WaypointRepository : MonoBehaviour
{
    [System.Serializable]
    public class WaypointEntry
    {
        public string id;
        public Transform point;
    }

    [Header("Waypoints")]
    [SerializeField] private List<WaypointEntry> waypoints = new();

    private Dictionary<string, Transform> _points;

    private void OnEnable()
    {
        Rebuild();
    }

    public void Rebuild()
    {
        _points = new Dictionary<string, Transform>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var e in waypoints)
        {
            if (e == null || e.point == null) continue;
            var key = (e.id ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(key)) continue;

            if (_points.ContainsKey(key))
                Debug.LogWarning($"[WaypointRegistry] Дубликат ID '{key}'. Используется первое вхождение.");
            else
                _points.Add(key, e.point);
        }
    }

    public bool TryGet(string id, out Transform t)
    {
        t = null;
        if (_points == null || _points.Count == 0)
        {
            Debug.LogWarning("[WaypointRegistry] Словарь пуст. Вызываю Rebuild().");
            Rebuild();
        }

        var key = (id ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("[WaypointRegistry] Пустой ID для TryGet().");
            return false;
        }

        var ok = _points.TryGetValue(key, out t);
        if (!ok)
        {
            Debug.LogWarning($"[WaypointRegistry] Не найден ID '{key}'. Доступные: {GetAvailableIds()}");
        }
        return ok;
    }

    private string GetAvailableIds()
    {
        if (_points == null || _points.Count == 0) return "<empty>";
        return string.Join(", ", _points.Keys);
    }
}