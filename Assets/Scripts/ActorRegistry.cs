using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface ISpeechService
{
    UniTask SpeakAsync(string speaker, string text, AudioClip voice, float minDisplaySeconds, CancellationToken token);
}

public interface IBotController
{
    UniTask MoveToAsync(Vector3 worldPosition, float stopDistance, CancellationToken token);
    UniTask PlayAnimationAsync(string stateName, float normalizedTime, bool waitForExit, CancellationToken token);
}

public sealed class ActorRegistry
{
    private readonly Dictionary<string, ISpeechService> _speech = new();
    private readonly Dictionary<string, IBotController> _bots = new();
    private readonly Dictionary<string, Transform> _roots = new();

    public void RegisterSpeech(string actorId, ISpeechService s)              { if (!string.IsNullOrWhiteSpace(actorId) && s != null) _speech[actorId] = s; }
    public void RegisterBot(string actorId, IBotController b)                 { if (!string.IsNullOrWhiteSpace(actorId) && b != null) _bots[actorId] = b; }
    public void RegisterRootTransform(string actorId, Transform t)            { if (!string.IsNullOrWhiteSpace(actorId) && t != null) _roots[actorId] = t; }

    public ISpeechService GetSpeech(string actorId)                           { _speech.TryGetValue(actorId, out var s); return s; }
    public IBotController GetBot(string actorId)                              { _bots.TryGetValue(actorId, out var b); return b; }
    public Transform GetRoot(string actorId)                                  { _roots.TryGetValue(actorId, out var t); return t; }
}
