using Cysharp.Threading.Tasks;
using UnityEngine;
using System;
using System.Linq;

namespace DefaultNamespace
{
    public enum WaitConditionMode
    {
        AllMustBeTrue,       // все ключи должны быть true
        AnyMustBeTrue,       // хотя бы один должен быть true
        AtLeastCountTrue,    // хотя бы N true
        AllMustBeFalse,      // все false
        AnyMustBeFalse,      // хотя бы один false
        AtLeastCountFalse    // хотя бы N false
    }

    [CreateAssetMenu(fileName = "WaitStateStep", menuName = "Conversation/Step/Wait State (Multi)")]
    public sealed class WaitStateStepSo : ConversationStepSo
    {
        [Header("Список ключей (prefix + objectId или просто свои имена)")]
        [Tooltip("Можно задать ключи напрямую, например: Held:KeyCard, Has:Battery, etc.")]
        public string[] stateKeys;

        [Header("Режим ожидания")]
        public WaitConditionMode waitMode = WaitConditionMode.AllMustBeTrue;

        [Tooltip("Для режимов 'AtLeastCount...' — сколько ключей должно соответствовать")]
        [Min(1)] public int threshold = 1;

        [Header("Настройки")]
        [Tooltip("Проверка будет выполняться циклически каждые N секунд.")]
        [Range(0.05f, 1f)]
        public float checkInterval = 0.2f;

        [Tooltip("Выводить в лог подробности ожидания")]
        public bool logVerbose = true;

        public override async UniTask Execute(ConversationContext context)
        {
            if (stateKeys == null || stateKeys.Length == 0)
            {
                Debug.LogWarning("[WaitStateStep] Нет ключей для ожидания.");
                return;
            }

            var hub = context.StateHub;
            var token = context.Token;

            Log($"Запуск ожидания ({waitMode}) для {stateKeys.Length} ключей...");

            switch (waitMode)
            {
                case WaitConditionMode.AllMustBeTrue:
                    await WaitUntilAsync(() => stateKeys.All(hub.IsTrue), context);
                    break;

                case WaitConditionMode.AnyMustBeTrue:
                    await WaitUntilAsync(() => stateKeys.Any(hub.IsTrue), context);
                    break;

                case WaitConditionMode.AtLeastCountTrue:
                    await WaitUntilAsync(() => stateKeys.Count(hub.IsTrue) >= threshold, context);
                    break;

                case WaitConditionMode.AllMustBeFalse:
                    await WaitUntilAsync(() => stateKeys.All(k => !hub.IsTrue(k)), context);
                    break;

                case WaitConditionMode.AnyMustBeFalse:
                    await WaitUntilAsync(() => stateKeys.Any(k => !hub.IsTrue(k)), context);
                    break;

                case WaitConditionMode.AtLeastCountFalse:
                    await WaitUntilAsync(() => stateKeys.Count(k => !hub.IsTrue(k)) >= threshold, context);
                    break;
            }

            Log("Условия выполнены!");
        }

        private async UniTask WaitUntilAsync(Func<bool> condition, ConversationContext context)
        {
            while (!context.Token.IsCancellationRequested)
            {
                if (condition())
                    return;

                await UniTask.Delay(TimeSpan.FromSeconds(checkInterval), cancellationToken: context.Token);
            }
        }

        private void Log(string msg)
        {
            if (logVerbose)
                Debug.Log($"[WaitStateStep] {msg}");
        }
    }
}
