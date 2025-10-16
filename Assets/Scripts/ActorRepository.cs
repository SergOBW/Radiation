using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IBotController
{
    UniTask MoveToAsync(Vector3 worldPosition, float stopDistance, CancellationToken token);
    UniTask PlayAnimationAsync(string stateName, float normalizedTime, bool waitForExit, CancellationToken token);
}

public sealed class ActorRepository : MonoBehaviour
{
    [Header("Список актёров на сцене")]
    [SerializeField] private List<ActorBinding> actorBindings = new();

    private readonly Dictionary<string, SpeechService> _speech = new();
    private readonly Dictionary<string, IBotController> _bots = new();
    private readonly Dictionary<string, Transform> _roots = new();

    private void OnEnable()
    {
        BuildCache();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Убираем null'ы и предупреждаем о дубликатах
        actorBindings.RemoveAll(a => a == null);

        var seen = new HashSet<string>();
        foreach (var binding in actorBindings)
        {
            if (binding == null) continue;
            var id = binding.ActorId;
            if (string.IsNullOrWhiteSpace(id)) continue;
            if (!seen.Add(id))
                Debug.LogWarning($"[ActorRepository] Дубликат actorId: '{id}'.", binding);
        }
    }
#endif

    private void BuildCache()
    {
        _speech.Clear();
        _bots.Clear();
        _roots.Clear();

        foreach (var binding in actorBindings)
        {
            if (binding == null) continue;

            var id = binding.ActorId;
            if (string.IsNullOrWhiteSpace(id)) continue;

            var speech = binding.Speech;
            var bot = binding.Bot;
            var root = binding.Root;

            if (speech != null) _speech[id] = speech;
            if (bot != null) _bots[id] = bot;
            if (root != null) _roots[id] = root;
        }

        Debug.Log($"[ActorRepository] Зарегистрировано {_speech.Count} Speech, {_bots.Count} Bot, {_roots.Count} Root.");
    }

    public SpeechService GetSpeech(string actorId)
    {
        _speech.TryGetValue(actorId, out var s);
        return s;
    }

    public IBotController GetBot(string actorId)
    {
        _bots.TryGetValue(actorId, out var b);
        return b;
    }

    public Transform GetRoot(string actorId)
    {
        _roots.TryGetValue(actorId, out var t);
        return t;
    }
}
