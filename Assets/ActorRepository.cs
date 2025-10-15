using System.Collections.Generic;
using UnityEngine;

public class ActorRepository : MonoBehaviour
{
    [SerializeField] private bool verboseLogs = false;

    private readonly Dictionary<string, ISpeechService> _speech = new();
    private readonly Dictionary<string, IBotController> _bots = new();
    private readonly Dictionary<string, Transform> _roots = new();

    private void OnDisable() { _speech.Clear(); _bots.Clear(); _roots.Clear(); }

    public void RegisterSpeech(string actorId, ISpeechService s) { if (!string.IsNullOrWhiteSpace(actorId) && s != null) { _speech[actorId] = s; if (verboseLogs) Debug.Log($"[ActorRepo] +Speech '{actorId}'"); } }
    public void RegisterBot(string actorId, IBotController b)    { if (!string.IsNullOrWhiteSpace(actorId) && b != null) { _bots[actorId] = b;   if (verboseLogs) Debug.Log($"[ActorRepo] +Bot '{actorId}'"); } }
    public void RegisterRootTransform(string actorId, Transform t){ if (!string.IsNullOrWhiteSpace(actorId) && t != null) { _roots[actorId] = t;  if (verboseLogs) Debug.Log($"[ActorRepo] +Root '{actorId}'={t.name}"); } }

    public void UnregisterSpeech(string actorId, ISpeechService s){ if (_speech.TryGetValue(actorId, out var cur) && ReferenceEquals(cur, s)) _speech.Remove(actorId); }
    public void UnregisterBot(string actorId, IBotController b)   { if (_bots.TryGetValue(actorId, out var cur) && ReferenceEquals(cur, b))   _bots.Remove(actorId); }
    public void UnregisterRootTransform(string actorId, Transform t){ if (_roots.TryGetValue(actorId, out var cur) && ReferenceEquals(cur, t)) _roots.Remove(actorId); }

    public ISpeechService GetSpeech(string actorId){ _speech.TryGetValue(actorId, out var s); return s; }
    public IBotController GetBot(string actorId)   { _bots.TryGetValue(actorId, out var b);  return b; }
    public Transform GetRoot(string actorId)       { _roots.TryGetValue(actorId, out var t); return t; }
}
