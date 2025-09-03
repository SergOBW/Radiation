using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Threading;

public sealed class SpeechService : MonoBehaviour, ISpeechService
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private TMP_Text subtitleText;

    public async UniTask SpeakAsync(string speaker, string text, AudioClip voice, float minDisplaySeconds, CancellationToken token)
    {
        if (subtitleText != null)
        {
            if (string.IsNullOrWhiteSpace(speaker))
                subtitleText.text = text;
            else
                subtitleText.text = $"{speaker}: {text}";
        }

        float minTime = Mathf.Max(0.0f, minDisplaySeconds);
        float playedTime = 0.0f;

        if (audioSource != null && voice != null)
        {
            audioSource.Stop();
            audioSource.clip = voice;
            audioSource.Play();

            while ((audioSource.isPlaying || playedTime < minTime) && !token.IsCancellationRequested)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);
                playedTime += Time.deltaTime;
            }
        }
        else
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(minTime), cancellationToken: token);
        }

        if (subtitleText != null)
            subtitleText.text = string.Empty;
    }
}