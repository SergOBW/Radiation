using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Conversation/Step/PlayAnim")]
public sealed class PlayAnimConversationStepSo : ConversationStepSo
{
    public string stateName;
    public float normalizedTime = 0.0f;
    public bool waitForExit = true;

    public override async UniTask Execute(ConversationContext context)
    {
        if (string.IsNullOrWhiteSpace(actorId) || string.IsNullOrWhiteSpace(stateName)) return;
        IBotController b = context.Registry.GetBot(actorId);
        if (b == null) return;
        await b.PlayAnimationAsync(stateName, normalizedTime, waitForExit, context.Token);
    }
}