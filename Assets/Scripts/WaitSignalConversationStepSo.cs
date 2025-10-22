using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Conversation/Step/WaitSignal")]
public sealed class WaitSignalConversationStepSo : ConversationStepSo
{
    public string signal;

    public override async UniTask Execute(ConversationContext context)
    {
        if (string.IsNullOrWhiteSpace(signal)) return;
        await context.scenarioSignals.Wait(signal, context.Token);
    }
}