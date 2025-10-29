using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public enum StateSetMode
{
    SetTrue,        // Установить True
    SetFalse,       // Установить False
    Toggle,         // Инвертировать текущее значение
    PulseTrue,      // True на pulseSeconds, затем вернуть обратно
    PulseFalse      // False на pulseSeconds, затем вернуть обратно
}

[CreateAssetMenu(menuName = "Conversation/Step/SetState")]
public sealed class SetStateConversationStepSo : ConversationStepSo
{
    [Header("Ключ состояния")]
    [SerializeField] private string stateKey = "CaseOpened";

    [Header("Режим изменения")]
    [SerializeField] private StateSetMode mode = StateSetMode.SetTrue;

    [Header("Тайминги")]
    [Tooltip("Задержка перед изменением состояния.")]
    [SerializeField] private float delayBeforeSeconds = 0f;

    [Tooltip("Для режимов Pulse: длительность импульса в секундах.")]
    [SerializeField] private float pulseSeconds = 0.5f;

    [Header("Ожидание подтверждения")]
    [Tooltip("Дождаться, пока ключ примет нужное значение (или вернётся после импульса).")]
    [SerializeField] private bool waitUntilApplied = false;

    [Header("Логи")]
    [SerializeField] private bool logVerbose = true;

    public override async UniTask Execute(ConversationContext context)
    {
        var key = (stateKey ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(key))
        {
            Log("stateKey пустой — ничего не делаем.");
            return;
        }

        var hub = context.StateHub;
        if (hub == null)
        {
            Debug.LogError("[SetStateStep] BoolStateHub в контексте отсутствует.");
            return;
        }

        var token = context.Token;

        if (delayBeforeSeconds > 0f)
        {
            Log($"Ждём delayBeforeSeconds={delayBeforeSeconds:0.##}с...");
            await UniTask.Delay(TimeSpan.FromSeconds(delayBeforeSeconds), cancellationToken: token);
        }

        try
        {
            switch (mode)
            {
                case StateSetMode.SetTrue:
                    hub.Set(key, true);
                    Log($"SetTrue: {key}=True");
                    if (waitUntilApplied) await hub.WaitUntilTrue(key, token);
                    break;

                case StateSetMode.SetFalse:
                    hub.Set(key, false);
                    Log($"SetFalse: {key}=False");
                    if (waitUntilApplied) await hub.WaitUntilFalse(key, token);
                    break;

                case StateSetMode.Toggle:
                {
                    bool newVal = !hub.IsTrue(key);
                    hub.Set(key, newVal);
                    Log($"Toggle: {key}={(newVal ? "True" : "False")}");
                    if (waitUntilApplied)
                    {
                        if (newVal) await hub.WaitUntilTrue(key, token);
                        else        await hub.WaitUntilFalse(key, token);
                    }
                    break;
                }

                case StateSetMode.PulseTrue:
                {
                    bool prev = hub.IsTrue(key);
                    hub.Set(key, true);
                    Log($"PulseTrue START: {key}=True на {pulseSeconds:0.##}с (prev={prev})");

                    if (waitUntilApplied) await hub.WaitUntilTrue(key, token);

                    if (pulseSeconds > 0f)
                        await UniTask.Delay(TimeSpan.FromSeconds(pulseSeconds), cancellationToken: token);

                    hub.Set(key, prev);
                    Log($"PulseTrue END: {key} возвращён в {prev}");

                    if (waitUntilApplied)
                    {
                        if (prev) await hub.WaitUntilTrue(key, token);
                        else      await hub.WaitUntilFalse(key, token);
                    }
                    break;
                }

                case StateSetMode.PulseFalse:
                {
                    bool prev = hub.IsTrue(key);
                    hub.Set(key, false);
                    Log($"PulseFalse START: {key}=False на {pulseSeconds:0.##}с (prev={prev})");

                    if (waitUntilApplied) await hub.WaitUntilFalse(key, token);

                    if (pulseSeconds > 0f)
                        await UniTask.Delay(TimeSpan.FromSeconds(pulseSeconds), cancellationToken: token);

                    hub.Set(key, prev);
                    Log($"PulseFalse END: {key} возвращён в {prev}");

                    if (waitUntilApplied)
                    {
                        if (prev) await hub.WaitUntilTrue(key, token);
                        else      await hub.WaitUntilFalse(key, token);
                    }
                    break;
                }
            }

        }
        catch (OperationCanceledException)
        {
            Log("Отменено по токену.");
        }
    }

    private void Log(string msg)
    {
        if (logVerbose)
            Debug.Log($"[SetStateStep] {msg}");
    }
}
