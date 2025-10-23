using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Notebook : MonoBehaviour
{
    private readonly Dictionary<string, float> _values = new();
    private readonly HashSet<string> _completed = new();

    public event Action Updated;

    public void SetValue(string pointName, float value)
    {
        _values[pointName] = value;
        _completed.Add(pointName);
        Updated?.Invoke();
    }

    public bool TryGetValue(string pointName, out float value)
        => _values.TryGetValue(pointName, out value);

    public bool IsCompleted(string pointName)
        => _completed.Contains(pointName);
}