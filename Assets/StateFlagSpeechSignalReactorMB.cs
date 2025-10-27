using UnityEngine;

using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

public sealed class StateFlagSpeechSignalReactorMB : MonoBehaviour
{
    [Header("ID актора для озвучки")]
    [SerializeField] private string speechActorId;

    [Header("Сигнал для прослушивания")]
    [SerializeField] private string listenSignal;

    [Header("Озвучка")]
    [SerializeField] private string speaker = "";
    [SerializeField] private string text = "Неверный предмет";
    [SerializeField] private AudioClip voice;
    [SerializeField] private float minDisplaySeconds = 0.6f;
    [SerializeField] private SpeechService speechService;

    private ActorRepository _repository;
    private SceneSignalHub _sceneHub;

    private CancellationTokenSource _cts;
    private bool _hasReacted;

    [Inject]
    public void Construct(SceneSignalHub sceneHub, ActorRepository repository)
    {
        _sceneHub   = sceneHub;
        _repository = repository;

        _hasReacted = false;
        _cts = new CancellationTokenSource();
        TryResolveSpeech();

        if (_sceneHub == null)
        {
            Debug.LogError("[StateFlagSpeechSignalReactorMB] SceneSignalHub is null!");
            return;
        }

        ListenSignalAsync(_cts.Token).Forget();
    }

    private void OnDisable()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private bool TryResolveSpeech()
    {
        if (speechService != null) return true;
        if (_repository == null) return false;
        speechService = _repository.GetSpeech(speechActorId);
        if (speechService == null)
        {
            Debug.LogError($"[StateFlagSpeechSignalReactorMB] SpeechService not found for actorId='{speechActorId}'");
            return false;
        }
        return true;
    }

    private async UniTaskVoid ListenSignalAsync(CancellationToken token)
    {
        try
        {
            await _sceneHub.Wait(listenSignal, token);

            if (token.IsCancellationRequested || _hasReacted)
                return;

            _hasReacted = true;

            Debug.Log($"[StateFlagSpeechSignalReactorMB] Received signal '{listenSignal}', start speaking");

            if (speechService != null)
            {
                await speechService.SpeakAsync(speaker, text, voice, minDisplaySeconds, token);
            }
            else
            {
                Debug.LogError("[StateFlagSpeechSignalReactorMB] SpeechService is null at speak time");
            }

            Debug.Log("[StateFlagSpeechSignalReactorMB] Speech finished, disabling script to prevent retrigger");
            enabled = false;
        }

        catch (System.Exception ex)
        {
            Debug.LogError($"[StateFlagSpeechSignalReactorMB] Error: {ex}");
        }
    }
}

