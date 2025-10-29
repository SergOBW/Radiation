using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using VContainer;

public sealed class ObjectsToggleOnSignal : MonoBehaviour
{
    [Header("Сигнал, который ждём")]
    [SerializeField] private string listenSignal = "CaseOpened";

    [Header("Сигнал, который издать после переключения")]
    [SerializeField] private string emitSignalAfter = "RadiometerAssembled";

    [Header("Объекты, которые ВЫКЛЮЧИТЬ при сигнале")]
    [SerializeField] private GameObject[] objectsToDisable;

    [Header("Объекты, которые ВКЛЮЧИТЬ при сигнале")]
    [SerializeField] private GameObject[] objectsToEnable;

    [Header("Поведение")]
    [SerializeField] private bool disableAtStart = true;
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private float delaySeconds = 0f;
    [SerializeField] private float emitDelaySeconds = 0f;
    [SerializeField] private bool emitBeforeDisablingSelf = false;

    [Header("Debug")]
    [SerializeField] private bool logVerbose = true;

    private SceneSignalHub _sceneHub;

    private CancellationTokenSource _cts;

    [Inject]
    public void Contruct(SceneSignalHub sceneHub)
    {
        _sceneHub = sceneHub;
        _cts = new CancellationTokenSource();
        ListenLoopAsync(_cts.Token).Forget();

        if (objectsToDisable != null && disableAtStart)
        {
            foreach (var g in objectsToDisable)
                if (g) { g.SetActive(false); }
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
        if (_sceneHub == null)
        {
            Debug.LogError("[ObjectsToggleOn] SceneSignalHub is null! Check DI setup.");
            return;
        }

        Log($"READY. Waiting '{listenSignal}' (triggerOnce={triggerOnce})");

        try
        {
            while (!token.IsCancellationRequested)
            {
                await _sceneHub.Wait(listenSignal, token);
                if (token.IsCancellationRequested) break;

                Log($"RECEIVED '{listenSignal}'");

                if (delaySeconds > 0f)
                {
                    Log($"Delay {delaySeconds:0.##}s before toggle");
                    await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: token);
                }

                bool willDisableSelf = InList(gameObject, objectsToDisable);
                if (emitBeforeDisablingSelf && willDisableSelf && !string.IsNullOrWhiteSpace(emitSignalAfter))
                {
                    Log($"Emit BEFORE disabling self: '{emitSignalAfter}'");
                    _sceneHub.EmitAll(emitSignalAfter);
                }

                ToggleObjects();

                if (!emitBeforeDisablingSelf && !string.IsNullOrWhiteSpace(emitSignalAfter))
                {
                    if (emitDelaySeconds > 0f)
                    {
                        Log($"Delay {emitDelaySeconds:0.##}s before emit-after");
                        await UniTask.Delay(TimeSpan.FromSeconds(emitDelaySeconds), cancellationToken: token);
                    }

                    _sceneHub.EmitAll(emitSignalAfter);
                    Log($"Emit AFTER toggle: '{emitSignalAfter}'");
                }

                if (triggerOnce) break;
            }
        }
        catch (OperationCanceledException) { /* норм */ }
        catch (Exception e)
        {
            Debug.LogError($"[ObjectsToggleOn] Error: {e}");
        }
    }

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

    private static bool InList(GameObject go, GameObject[] list)
    {
        if (!go || list == null) return false;
        for (int i = 0; i < list.Length; i++)
            if (list[i] == go) return true;
        return false;
    }

    private void Log(string msg)
    {
        if (logVerbose)
            Debug.Log($"[ObjectsToggleOn] {msg}");
    }
}
