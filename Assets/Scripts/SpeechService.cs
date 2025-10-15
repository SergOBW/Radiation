using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Threading;

public sealed class SpeechService : MonoBehaviour, ISpeechService
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private TMP_Text subtitleText;

    // локальный CTS для всех текущих/будущих SpeakAsync
    private CancellationTokenSource _lifetimeCts;

    private void Awake()
    {
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }
        _lifetimeCts = new CancellationTokenSource();
    }

    private void OnDisable()  => StopAllAudioAndCancel("OnDisable");
    private void OnDestroy()  => StopAllAudioAndCancel("OnDestroy");
    private void OnApplicationQuit() => StopAllAudioAndCancel("OnApplicationQuit");

    private void StopAllAudioAndCancel(string reason)
    {
        _lifetimeCts?.Cancel();
        _lifetimeCts?.Dispose();
        _lifetimeCts = new CancellationTokenSource();

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }
        if (subtitleText != null)
            subtitleText.text = string.Empty;
    }

    public async UniTask SpeakAsync(string speaker, string text, AudioClip voice, float minDisplaySeconds, CancellationToken token)
    {
        if (subtitleText != null)
            subtitleText.text = string.IsNullOrWhiteSpace(speaker) ? text : $"{speaker}: {text}";

        // связка внешнего токена сценария + локального токена сервиса
        using (var linked = CancellationTokenSource.CreateLinkedTokenSource(token, _lifetimeCts.Token))
        {
            var ct = linked.Token;
            float minTime = Mathf.Max(0f, minDisplaySeconds);
            float played = 0f;

            try
            {
                if (audioSource != null && voice != null)
                {
                    audioSource.Stop();
                    audioSource.clip = voice;
                    audioSource.time = 0f;
                    audioSource.Play();

                    while (!ct.IsCancellationRequested && (audioSource.isPlaying || played < minTime))
                    {
                        await UniTask.Yield(PlayerLoopTiming.Update, ct);
                        played += Time.deltaTime;
                    }
                }
                else
                {
                    await UniTask.Delay(System.TimeSpan.FromSeconds(minTime), cancellationToken: ct);
                }
            }
            finally
            {
                if (audioSource != null)
                {
                    audioSource.Stop();
                    audioSource.clip = null;
                }
                if (subtitleText != null)
                    subtitleText.text = string.Empty;
            }
        }
    }
}
