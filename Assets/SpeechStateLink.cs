using UnityEngine;

[DisallowMultipleComponent]
public sealed class SpeechStateLink : MonoBehaviour
{
    [SerializeField] private SpeechService speech;
    [SerializeField] private CharacterStateController state;

    private void Awake()
    {
        if (speech == null) speech = GetComponent<SpeechService>();
        if (state == null) state = GetComponent<CharacterStateController>();
    }

    private void OnEnable()
    {
        if (speech != null)
        {
            speech.SpeakingStarted += OnSpeakingStarted;
            speech.SpeakingEnded += OnSpeakingEnded;

            if (speech.IsSpeaking)
                OnSpeakingStarted();
        }
    }

    private void OnDisable()
    {
        if (speech != null)
        {
            speech.SpeakingStarted -= OnSpeakingStarted;
            speech.SpeakingEnded -= OnSpeakingEnded;
        }
    }

    private void OnSpeakingStarted()
    {
        if (state != null) state.StartTalking();
    }

    private void OnSpeakingEnded()
    {
        if (state != null) state.StopTalking();
    }
}