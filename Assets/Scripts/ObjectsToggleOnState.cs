using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using System.Threading;
using VContainer;

public sealed class ObjectsToggleOnState : MonoBehaviour
{
    public enum TriggerEdge
    {
        WhenTrue,   // сработать при переходе: count >= requiredTrueCount
        WhenFalse,  // сработать при переходе: count <  requiredTrueCount
    }

    [Header("Ключи BoolStateHub и условие срабатывания")]
    [SerializeField] private string[] stateKeys = new[] { "CaseOpened", "AnotherKey" };

    [Tooltip("Сколько ключей должно быть True для срабатывания (для 'включено' — порог). " +
             "Напр.: 2 означает «когда как минимум два ключа true». " +
             "Для ALL — поставьте равным числу ключей, для ANY — 1.")]
    [Min(1)]
    [SerializeField] private int requiredTrueCount = 2;
    [SerializeField] private bool disableAtStart = true;

    [SerializeField] private TriggerEdge triggerOn = TriggerEdge.WhenTrue;

    [Header("Объекты, которые ВЫКЛЮЧИТЬ при триггере")]
    [SerializeField] private GameObject[] objectsToDisable;

    [Header("Объекты, которые ВКЛЮЧИТЬ при триггере")]
    [SerializeField] private GameObject[] objectsToEnable;

    [Header("Поведение")]
    [SerializeField] private bool triggerOnce = true;

    [Tooltip("Если уже в нужном состоянии при старте — сработать сразу.")]
    [SerializeField] private bool triggerIfAlreadyInStateOnStart = false;

    [SerializeField] private float delaySeconds = 0f;

    [Header("Debug")]
    [SerializeField] private bool logVerbose = true;

    private BoolStateHub _stateHub;
    private CancellationTokenSource _cts;

    [Inject]
    public void Construct(BoolStateHub stateHub)
    {
        _stateHub = stateHub;
        _cts = new CancellationTokenSource();
        ListenLoopAsync(_cts.Token).Forget();

        if (objectsToDisable != null && disableAtStart)
        {
            foreach (var g in objectsToDisable)
                if (g) g.SetActive(false);
        }
    }

    private void OnDisable()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async UniTaskVoid ListenLoopAsync(CancellationToken token)
    {
        if (_stateHub == null)
        {
            Debug.LogError("[ObjectsToggleOnState] BoolStateHub is null! Check DI setup.");
            return;
        }

        if (stateKeys == null || stateKeys.Length == 0)
        {
            Debug.LogError("[ObjectsToggleOnState] stateKeys is empty — нечего отслеживать.");
            return;
        }

        // Нормализуем ключи (убираем пустые)
        stateKeys = stateKeys.Where(k => !string.IsNullOrWhiteSpace(k)).ToArray();
        if (stateKeys.Length == 0)
        {
            Debug.LogError("[ObjectsToggleOnState] Все ключи пустые/пробельные.");
            return;
        }

        // Порог не может быть больше количества ключей
        requiredTrueCount = Mathf.Clamp(requiredTrueCount, 1, stateKeys.Length);

        bool targetIsTrueSide = (triggerOn == TriggerEdge.WhenTrue);

        Log($"READY. Keys=[{string.Join(", ", stateKeys)}], threshold={requiredTrueCount}, " +
            $"edge={(targetIsTrueSide ? "WhenTrue(>=)" : "WhenFalse(<)")}, " +
            $"triggerOnce={triggerOnce}, triggerIfAlreadyInStateOnStart={triggerIfAlreadyInStateOnStart}");

        try
        {
            // Если не хотим мгновенного срабатывания при уже совпадающем состоянии — ждём сначала противоположного края
            bool nowMatches = CurrentMatchesTarget(targetIsTrueSide);
            if (!triggerIfAlreadyInStateOnStart && nowMatches)
            {
                Log($"Current state ALREADY matches target. Waiting for opposite edge first...");
                await WaitUntilOppositeAsync(targetIsTrueSide, token);
            }

            while (!token.IsCancellationRequested)
            {
                // Ждём целевой край
                await WaitUntilTargetAsync(targetIsTrueSide, token);
                if (token.IsCancellationRequested) break;

                Log($"RECEIVED target edge. CurrentTrueCount={GetCurrentTrueCount()} (threshold={requiredTrueCount}).");

                if (delaySeconds > 0f)
                {
                    Log($"Delay {delaySeconds:0.##}s before toggle");
                    await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: token);
                }

                ToggleObjects();

                if (triggerOnce) break;

                // После срабатывания ждём отход на противоположный край, чтобы не «дребезжало»
                await WaitUntilOppositeAsync(targetIsTrueSide, token);
            }
        }
        catch (OperationCanceledException) { /* нормальная отмена */ }
        catch (Exception e)
        {
            Debug.LogError($"[ObjectsToggleOnState] Error: {e}");
        }
    }

    // === МНОГОКЛЮЧЕВАЯ ЛОГИКА ОЖИДАНИЯ ===

    // Текущая «целевость»: для WhenTrue — (count >= N), для WhenFalse — (count < N)
    private bool CurrentMatchesTarget(bool targetIsTrueSide)
    {
        int c = GetCurrentTrueCount();
        return targetIsTrueSide ? (c >= requiredTrueCount) : (c < requiredTrueCount);
    }

    private int GetCurrentTrueCount()
    {
        int count = 0;
        foreach (var key in stateKeys)
        {
            if (_stateHub.IsTrue(key))
                count++;
        }
        return count;
    }

    private async UniTask WaitUntilTargetAsync(bool targetIsTrueSide, CancellationToken token)
    {
        if (targetIsTrueSide)
            await WaitUntilCountAtLeast(requiredTrueCount, token);
        else
            await WaitUntilCountBelow(requiredTrueCount, token);
    }

    private async UniTask WaitUntilOppositeAsync(bool targetIsTrueSide, CancellationToken token)
    {
        // Противоположность целевому краю
        if (targetIsTrueSide)
            await WaitUntilCountBelow(requiredTrueCount, token);
        else
            await WaitUntilCountAtLeast(requiredTrueCount, token);
    }

    private async UniTask WaitUntilCountAtLeast(int threshold, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            int current = GetCurrentTrueCount();
            if (current >= threshold) return;

            // Ждём, пока ЛЮБОЙ из «ложных» ключей станет true
            var waiters = stateKeys
                .Where(k => !_stateHub.IsTrue(k))
                .Select(k => _stateHub.WaitUntilTrue(k, token))
                .ToArray();

            if (waiters.Length == 0)
            {
                // Теоретически уже достигли (или нет ключей для ожидания) — защитная проверка
                return;
            }

            await UniTask.WhenAny(waiters);
            // Затем цикл проверит count заново.
        }
    }

    private async UniTask WaitUntilCountBelow(int threshold, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            int current = GetCurrentTrueCount();
            if (current < threshold) return;

            // Ждём, пока ЛЮБОЙ из «истинных» ключей станет false
            var waiters = stateKeys
                .Where(k => _stateHub.IsTrue(k))
                .Select(k => _stateHub.WaitUntilFalse(k, token))
                .ToArray();

            if (waiters.Length == 0)
            {
                // Нет истинных ключей для ожидания — значит уже ниже порога
                return;
            }

            await UniTask.WhenAny(waiters);
            // Затем цикл проверит count заново.
        }
    }

    // === Переключение объектов ===

    private void ToggleObjects()
    {
        int off = 0, on = 0;

        if (objectsToDisable != null)
        {
            foreach (var g in objectsToDisable)
                if (g) { g.SetActive(false); off++; }
        }

        if (objectsToEnable != null)
        {
            foreach (var g in objectsToEnable)
                if (g) { g.SetActive(true); on++; }
        }

        Log($"Toggled: Disabled={off}, Enabled={on}");
    }

    // === Утилиты ===

    private void Log(string msg)
    {
        if (logVerbose)
            Debug.Log($"[ObjectsToggleOnState] {msg}");
    }
}
