using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DefaultNamespace
{
    [CreateAssetMenu(fileName = "WaitStateStep", menuName = "Conversation/Step/Wait State")]
    public sealed class WaitStateStepSo : ConversationStepSo
    {
        [Header("State key = prefix + objectId")]
        public string keyPrefix = "Held:";   // например "Held:"
        public string objectId = "KeyCard";  // например "KeyCard" или instance id
        [Header("What to wait")]
        public bool waitForTrue = true;      // true = ждём пока станет true, false = ждём пока станет false

        public override async UniTask Execute(ConversationContext context)
        {
            var key = keyPrefix + objectId;

            if (waitForTrue)
            {
                // мгновенно вернётся, если уже true
                await context.StateHub.WaitUntilTrue(key, context.Token);
            }
            else
            {
                await context.StateHub.WaitUntilFalse(key, context.Token);
            }
        }
    }
}