using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

public sealed class SpeechService : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private TMP_Text subtitleText;

    private CancellationTokenSource _lifetimeCts;

    public bool IsSpeaking { get; private set; }
    public event Action SpeakingStarted;
    public event Action SpeakingEnded;

    private void Awake()
    {
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }
        _lifetimeCts = new CancellationTokenSource();
    }

    private void OnDisable()         { StopAllAudioAndCancel("OnDisable"); }
    private void OnDestroy()         { StopAllAudioAndCancel("OnDestroy"); }
    private void OnApplicationQuit() { StopAllAudioAndCancel("OnApplicationQuit"); }

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

        if (IsSpeaking)
        {
            IsSpeaking = false;
            SpeakingEnded?.Invoke();
        }
    }

    public async UniTask SpeakAsync(string speaker, string text, AudioClip voice, float minDisplaySeconds, CancellationToken token)
    {
        if (subtitleText != null)
            subtitleText.text = string.IsNullOrWhiteSpace(speaker) ? text : speaker + ": " + text;

        using (var linked = CancellationTokenSource.CreateLinkedTokenSource(token, _lifetimeCts.Token))
        {
            CancellationToken ct = linked.Token;
            float minTime = Mathf.Max(0f, minDisplaySeconds);
            float played = 0f;

            try
            {
                IsSpeaking = true;
                SpeakingStarted?.Invoke();

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

                if (IsSpeaking)
                {
                    IsSpeaking = false;
                    SpeakingEnded?.Invoke();
                }
            }
        }
    }
}
