using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Conversation/Step/WaitSeconds")]
public sealed class WaitSecondsConversationStepSo : ConversationStepSo
{
    public float seconds = 1.0f;

    public override async UniTask Execute(ConversationContext context)
    {
        await Cysharp.Threading.Tasks.UniTask.Delay(
            System.TimeSpan.FromSeconds(seconds),
            cancellationToken: context.Token);
    }
}