using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IBotController
{
    UniTask MoveToAsync(Vector3 worldPosition, float stopDistance, CancellationToken token);
}

public sealed class ActorRepository : MonoBehaviour
{
    [Header("Список актёров на сцене")]
    [SerializeField] private List<ActorBinding> actorBindings = new();

    private readonly Dictionary<string, SpeechService> _speech = new();
    private readonly Dictionary<string, IBotController> _bots = new();
    private readonly Dictionary<string, Transform> _roots = new();
    private readonly Dictionary<string, BotAnimator> _animators = new();

    private void OnEnable()
    {
        BuildCache();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        actorBindings.RemoveAll(a => a == null);

        HashSet<string> seen = new HashSet<string>();
        for (int i = 0; i < actorBindings.Count; i++)
        {
            ActorBinding binding = actorBindings[i];
            if (binding == null) continue;
            string id = binding.ActorId;
            if (string.IsNullOrWhiteSpace(id)) continue;
            if (!seen.Add(id)) Debug.LogWarning("[ActorRepository] Дубликат actorId: '" + id + "'.", binding);
        }
    }
#endif

    private void BuildCache()
    {
        _speech.Clear();
        _bots.Clear();
        _roots.Clear();
        _animators.Clear();

        for (int i = 0; i < actorBindings.Count; i++)
        {
            ActorBinding binding = actorBindings[i];
            if (binding == null) continue;

            string id = binding.ActorId;
            if (string.IsNullOrWhiteSpace(id)) continue;

            SpeechService speech = binding.Speech;
            IBotController bot = binding.Bot;
            Transform root = binding.Root;

            if (speech != null) _speech[id] = speech;
            if (bot != null) _bots[id] = bot;
            if (root != null) _roots[id] = root;

            BotAnimator animator = TryResolveAnimator(binding);
            if (animator != null) _animators[id] = animator;
        }

        Debug.Log("[ActorRepository] Зарегистрировано " + _speech.Count + " Speech, " + _bots.Count + " Bot, " + _roots.Count + " Root, " + _animators.Count + " Animator.");
    }

    private BotAnimator TryResolveAnimator(ActorBinding binding)
    {
        if (binding == null) return null;

        if (binding.Root != null)
        {
            BotAnimator byRoot = binding.Root.GetComponentInChildren<BotAnimator>();
            if (byRoot != null) return byRoot;
        }

        if (binding.Bot is Component botComponent && botComponent != null)
        {
            BotAnimator byBot = botComponent.GetComponentInChildren<BotAnimator>();
            if (byBot != null) return byBot;
        }

        GameObject actorGo = Find(binding.ActorId);
        if (actorGo != null)
        {
            BotAnimator byGo = actorGo.GetComponentInChildren<BotAnimator>();
            if (byGo != null) return byGo;
        }

        return null;
    }

    public SpeechService GetSpeech(string actorId)
    {
        _speech.TryGetValue(actorId, out SpeechService result);
        return result;
    }

    public IBotController GetBot(string actorId)
    {
        _bots.TryGetValue(actorId, out IBotController result);
        return result;
    }

    public Transform GetRoot(string actorId)
    {
        _roots.TryGetValue(actorId, out Transform result);
        return result;
    }

    public BotAnimator GetBotAnimator(string actorId)
    {
        if (_animators.TryGetValue(actorId, out BotAnimator cached)) return cached;

        GameObject go = Find(actorId);
        if (go == null) return null;

        BotAnimator animator = go.GetComponentInChildren<BotAnimator>();
        if (animator != null) _animators[actorId] = animator;
        return animator;
    }

    private GameObject Find(string actorId)
    {
        if (string.IsNullOrWhiteSpace(actorId)) return null;

        if (_roots.TryGetValue(actorId, out Transform fromRoot) && fromRoot != null)
            return fromRoot.gameObject;

        for (int i = 0; i < actorBindings.Count; i++)
        {
            ActorBinding binding = actorBindings[i];
            if (binding == null) continue;
            if (binding.ActorId != actorId) continue;

            if (binding.Root != null) return binding.Root.gameObject;
            if (binding.Bot is Component botComponent && botComponent != null) return botComponent.gameObject;
            if (binding.Speech != null) return binding.Speech.gameObject;
        }

        return null;
    }
}
