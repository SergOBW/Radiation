using UnityEngine;

public sealed class ActorBinding : MonoBehaviour
{
    [SerializeField] private string actorId;
    [SerializeField] private SpeechService speechService;
    [SerializeField] private BotController botController;
    [SerializeField] private Transform rootTransform;

    private ActorRegistry _registry;

    [VContainer.Inject]
    public void Construct(ActorRegistry registry) => _registry = registry;

    private void Awake()
    {
        if (_registry == null) return;
        if (speechService != null) _registry.RegisterSpeech(actorId, speechService);
        if (botController != null) _registry.RegisterBot(actorId, botController);
        _registry.RegisterRootTransform(actorId, rootTransform != null ? rootTransform : transform);
    }
}