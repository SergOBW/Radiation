using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Conversation/Step/EmitSignal")]
public sealed class EmitSignalConversationStepSo : ConversationStepSo
{
    public string signal;

    public override UniTask Execute(ConversationContext context)
    {
        if (!string.IsNullOrWhiteSpace(signal)) context.Signals.Emit(signal);
        return UniTask.CompletedTask;
    }
}