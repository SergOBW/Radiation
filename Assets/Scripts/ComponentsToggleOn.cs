using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using VContainer;

public sealed class ComponentsToggleOn : MonoBehaviour
{
    [Header("Сигнал, который ждём")]
    [SerializeField] private string listenSignal = "RadiometerAssembled";

    [Header("Сигнал, который издать после переключения (опционально)")]
    [SerializeField] private string emitSignalAfter = "RadiometerReady";

    [Header("Компоненты, которые нужно ВЫКЛЮЧИТЬ при сигнале")]
    [SerializeField] private Collider[] componentsToDisable;

    [Header("Компоненты, которые нужно ВКЛЮЧИТЬ при сигнале")]
    [SerializeField] private Collider[] componentsToEnable;

    [Header("Поведение")]
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private float delaySeconds = 0f;
    [SerializeField] private float emitDelaySeconds = 0f;

    [Inject] private SceneSignalHub _scenarioSignalHub;

    private CancellationTokenSource _cts;

    private void Start()
    {
        _cts = new CancellationTokenSource();
        RunnerAsync(_cts.Token).Forget();
    }

    private void OnDisable()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async UniTaskVoid RunnerAsync(CancellationToken token)
    {
        Debug.Log($"[ComponentsToggleOn] RunnerAsync");
        // 1) Если DI ещё не вложил hub — попробуем подождать немного.
        if (_scenarioSignalHub == null)
        {
            Debug.LogError("[ComponentsToggleOn] No SignalHub available. Abort.");
            return;
        }

        Debug.Log($"[ComponentsToggleOn] Waiting '{listenSignal}' (triggerOnce={triggerOnce})");

        try
        {
            while (!token.IsCancellationRequested)
            {
                await _scenarioSignalHub.Wait(listenSignal, token);
                if (token.IsCancellationRequested) break;

                Debug.Log($"[ComponentsToggleOn] Received '{listenSignal}'");

                if (delaySeconds > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: token);

                if (token.IsCancellationRequested) break;

                ToggleComponents();

                if (!string.IsNullOrWhiteSpace(emitSignalAfter))
                {
                    if (emitDelaySeconds > 0f)
                        await UniTask.Delay(TimeSpan.FromSeconds(emitDelaySeconds), cancellationToken: token);

                    _scenarioSignalHub.Emit(emitSignalAfter);
                    Debug.Log($"[ComponentsToggleOn] Emitted '{emitSignalAfter}'");
                }

                if (triggerOnce) break;
                Debug.Log($"[ComponentsToggleOn] Loop again. Waiting '{listenSignal}'");
            }
        }
        catch (OperationCanceledException) { /* ok */ }
        catch (Exception e)
        {
            Debug.LogError($"[ComponentsToggleOn] Error: {e}");
        }
    }
    private void ToggleComponents()
    {
        int offCount = 0, onCount = 0;

        if (componentsToDisable != null)
        {
            foreach (var comp in componentsToDisable)
                if (comp) { comp.enabled = false; offCount++; }
        }

        if (componentsToEnable != null)
        {
            foreach (var comp in componentsToEnable)
                if (comp) { comp.enabled = true; onCount++; }
        }

        Debug.Log($"[ComponentsToggleOn] '{listenSignal}' → Disabled {offCount}, Enabled {onCount}");
    }
}
