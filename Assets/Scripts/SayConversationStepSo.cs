using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Conversation/Step/Say")]
public sealed class SayConversationStepSo : ConversationStepSo
{
    public string speakerName;
    [TextArea] public string text;
    public AudioClip voice;
    [Min(0f)] public float minDisplaySeconds = 1.0f;

    public override async UniTask Execute(ConversationContext context)
    {
        if (string.IsNullOrWhiteSpace(actorId)) return;
        var speech = context.Registry.GetSpeech(actorId);
        if (speech == null) return;

        await speech.SpeakAsync(speakerName, text, voice, minDisplaySeconds, context.Token);
    }
}