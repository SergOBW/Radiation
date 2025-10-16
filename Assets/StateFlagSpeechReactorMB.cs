using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

public sealed class StateFlagSpeechReactorMB : MonoBehaviour
{
    [Header("Чей SpeechService брать из репозитория")]
    [SerializeField] private string speechActorId;

    [Header("Ключ состояния (StateHub)")]
    [SerializeField] private string keyPrefix = "Held:";
    [SerializeField] private string objectId = "Any";

    [Header("Условие ожидания")]
    [SerializeField] private bool reactWhenTrue = true;
    [SerializeField] private bool retriggerOnEveryChange = false;
    [SerializeField] private float retriggerCooldownSeconds = 0.0f;

    [Header("Озвучка")]
    [SerializeField] private string speaker = "";
    [SerializeField] private string text = "Неверный предмет";
    [SerializeField] private AudioClip voice;
    [SerializeField] private float minDisplaySeconds = 0.6f;

    private ActorRepository _repository;
    private BoolStateHub _stateHub;
    [SerializeField]private SpeechService speechService;

    private CancellationTokenSource _cts;
    private bool _isRunning;

    public void SetRepository(ActorRepository repository)
    {
        _repository = repository;
        Debug.Log($"[Reactor:{name}] repo set");
        TryStartIfReady();
    }

    public void SetStateHub(BoolStateHub hub)
    {
        _stateHub = hub;
        Debug.Log($"[Reactor:{name}] hub set");
        TryStartIfReady();
    }

    private string Key { get { return keyPrefix + objectId; } }

    private void OnEnable()
    {
        Debug.Log($"[Reactor:{name}] OnEnable");
        TryStartIfReady();
    }

    private void OnDisable()
    {
        Debug.Log($"[Reactor:{name}] OnDisable");
        _isRunning = false;

        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        speechService = null;
    }

    private void EnsureCts()
    {
        if (_cts != null) return;

        if (!isActiveAndEnabled)
        {
            Debug.Log($"[Reactor:{name}] EnsureCts: component not active yet");
            return;
        }

        _cts = new CancellationTokenSource();
        Debug.Log($"[Reactor:{name}] CTS created");
    }

    private void TryStartIfReady()
    {
        if (_isRunning)
        {
            Debug.Log($"[Reactor:{name}] already running");
            return;
        }

        EnsureCts();
        if (_cts == null)
        {
            Debug.Log($"[Reactor:{name}] CTS is null, will retry on enable");
            return;
        }

        if (!IsDependenciesReady())
        {
            LogDependenciesState();
            return;
        }

        if (!TryResolveSpeech())
        {
            Debug.LogError($"[Reactor:{name}] SpeechService not found for actorId='{speechActorId}'");
            return;
        }

        _isRunning = true;
        Debug.Log($"[Reactor:{name}] START key='{Key}', actorId='{speechActorId}', whenTrue={reactWhenTrue}, retrigger={retriggerOnEveryChange}");
        RunAsync(_cts.Token).Forget();
    }

    private bool IsDependenciesReady()
    {
        if (_repository == null) return false;
        if (_stateHub == null) return false;
        if (string.IsNullOrWhiteSpace(speechActorId)) return false;
        return true;
    }

    private void LogDependenciesState()
    {
        string repo = _repository != null ? "OK" : "NULL";
        string hub = _stateHub != null ? "OK" : "NULL";
        string id = !string.IsNullOrWhiteSpace(speechActorId) ? $"'{speechActorId}'" : "EMPTY";
        Debug.Log($"[Reactor:{name}] waiting deps -> repo={repo}, hub={hub}, id={id}");
    }

    private bool TryResolveSpeech()
    {
        if (speechService != null) return true;
        speechService = _repository.GetSpeech(speechActorId);
        if (speechService != null)
        {
            Debug.Log($"[Reactor:{name}] speech resolved for id='{speechActorId}'");
            return true;
        }
        return false;
    }

    private async UniTaskVoid RunAsync(CancellationToken token)
    {
        if (!IsDependenciesReady() || speechService == null)
        {
            _isRunning = false;
            Debug.Log($"[Reactor:{name}] abort start: deps not ready");
            return;
        }

        while (!token.IsCancellationRequested)
        {
            bool current = _stateHub.IsTrue(Key);
            Debug.Log($"[Reactor:{name}] wait loop key='{Key}', current={current}, whenTrue={reactWhenTrue}");

            if (reactWhenTrue)
                await _stateHub.WaitUntilTrue(Key, token);
            else
                await _stateHub.WaitUntilFalse(Key, token);

            if (token.IsCancellationRequested) break;

            Debug.Log($"[Reactor:{name}] TRIGGER -> speak '{text}' (speaker='{speaker}', clip={(voice? voice.name : "null")})");
            await speechService.SpeakAsync(speaker, text, voice, minDisplaySeconds, token);
            Debug.Log($"[Reactor:{name}] speak done");

            if (!retriggerOnEveryChange)
            {
                Debug.Log($"[Reactor:{name}] oneshot complete");
                break;
            }

            if (retriggerCooldownSeconds > 0f)
            {
                Debug.Log($"[Reactor:{name}] cooldown {retriggerCooldownSeconds:0.###}s");
                await UniTask.Delay(System.TimeSpan.FromSeconds(retriggerCooldownSeconds), cancellationToken: token);
                if (token.IsCancellationRequested) break;
            }

            Debug.Log($"[Reactor:{name}] wait opposite before retrigger");
            if (reactWhenTrue)
                await _stateHub.WaitUntilFalse(Key, token);
            else
                await _stateHub.WaitUntilTrue(Key, token);
        }

        _isRunning = false;
        Debug.Log($"[Reactor:{name}] STOP");
    }
}
