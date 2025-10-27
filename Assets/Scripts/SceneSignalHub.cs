using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine; // Для Debug.Log

public sealed class SceneSignalHub
{
    private readonly Dictionary<string, LinkedList<UniTaskCompletionSource<bool>>> _waiters = new();
    private readonly Dictionary<string, int> _pendingCounts = new();

    public void Emit(string signal)
    {
        Debug.Log($"Emit called with signal: {signal}");
        if (string.IsNullOrWhiteSpace(signal))
        {
            Debug.Log("Signal is null or whitespace, returning.");
            return;
        }

        if (_waiters.TryGetValue(signal, out var list) && list is { Count: > 0 })
        {
            while (list.Count > 0)
            {
                var src = list.First.Value;
                list.RemoveFirst();
                if (!src.Task.Status.IsCompleted())
                {
                    Debug.Log($"Emit: Set result for one waiter on signal: {signal}");
                    src.TrySetResult(true);
                    return;
                }
            }
        }
        _pendingCounts[signal] = (_pendingCounts.TryGetValue(signal, out var cnt) ? cnt : 0) + 1;
        Debug.Log($"Emit: No waiters found, incremented pending count for signal: {signal} to {_pendingCounts[signal]}");
    }

    public void EmitAll(string signal)
    {
        Debug.Log($"EmitAll called with signal: {signal}");
        if (string.IsNullOrWhiteSpace(signal))
        {
            Debug.Log("Signal is null or whitespace, returning.");
            return;
        }

        if (_waiters.TryGetValue(signal, out var list) && list is { Count: > 0 })
        {
            foreach (var src in list)
            {
                if (!src.Task.Status.IsCompleted())
                {
                    Debug.Log($"EmitAll: Set result for a waiter on signal: {signal}");
                    src.TrySetResult(true);
                }
            }
            list.Clear();
        }
        else
        {
            Debug.Log("EmitAll: No waiters found to notify.");
        }
        // Не буферизуем на будущее
    }

    public async UniTask Wait(string signal, CancellationToken token)
    {
        Debug.Log($"Wait called with signal: {signal}");
        if (string.IsNullOrWhiteSpace(signal))
        {
            Debug.Log("Signal is null or whitespace, returning.");
            return;
        }

        if (_pendingCounts.TryGetValue(signal, out var cnt) && cnt > 0)
        {
            _pendingCounts[signal] = cnt - 1;
            Debug.Log($"Wait: Consumed pending emit for signal: {signal}, remaining count: {_pendingCounts[signal]}");
            return;
        }

        var src = new UniTaskCompletionSource<bool>();
        if (!_waiters.TryGetValue(signal, out var list) || list == null)
        {
            list = new LinkedList<UniTaskCompletionSource<bool>>();
            _waiters[signal] = list;
        }
        var node = list.AddLast(src);
        Debug.Log($"Wait: Added new waiter for signal: {signal}. Total waiters: {list.Count}");

        using (token.Register(() =>
        {
            if (node.List != null) node.List.Remove(node);
            src.TrySetCanceled();
            Debug.Log($"Wait: Cancelled waiter for signal: {signal}");
        }))
        {
            await src.Task;
            Debug.Log($"Wait: Waiter on signal {signal} completed.");
        }
    }

    /// <summary>Сбросить только буфер эмитов; активные Wait остаются.</summary>
    public void ClearPending()
    {
        Debug.Log($"ClearPending called. Clearing {_pendingCounts.Count} pending signal counts.");
        _pendingCounts.Clear();
    }
}
