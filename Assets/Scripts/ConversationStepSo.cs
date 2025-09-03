using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class ConversationStepSo : ScriptableObject
{
    public string actorId;
    public abstract UniTask Execute(ConversationContext context);

}