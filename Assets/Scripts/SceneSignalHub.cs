using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;

/// <summary>
/// Хаб для сцены: много слушателей. Emit будит одного, EmitAll — всех текущих.
/// Буферизует эмиты (счётчик), ClearPending() НЕ трогает активные ожидания.
/// </summary>
public sealed class SceneSignalHub
{
    private readonly Dictionary<string, LinkedList<UniTaskCompletionSource<bool>>> _waiters = new();
    private readonly Dictionary<string, int> _pendingCounts = new();

    public void Emit(string signal)
    {
        if (string.IsNullOrWhiteSpace(signal)) return;

        if (_waiters.TryGetValue(signal, out var list) && list is { Count: > 0 })
        {
            while (list.Count > 0)
            {
                var src = list.First.Value;
                list.RemoveFirst();
                if (!src.Task.Status.IsCompleted())
                {
                    src.TrySetResult(true);
                    return;
                }
            }
        }
        _pendingCounts[signal] = (_pendingCounts.TryGetValue(signal, out var cnt) ? cnt : 0) + 1;
    }

    public void EmitAll(string signal)
    {
        if (string.IsNullOrWhiteSpace(signal)) return;
        if (_waiters.TryGetValue(signal, out var list) && list is { Count: > 0 })
        {
            foreach (var src in list)
                if (!src.Task.Status.IsCompleted())
                    src.TrySetResult(true);
            list.Clear();
        }
        // без буферизации «на будущее»
    }

    public async UniTask Wait(string signal, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(signal)) return;

        if (_pendingCounts.TryGetValue(signal, out var cnt) && cnt > 0)
        {
            _pendingCounts[signal] = cnt - 1; // мгновенно съели буфер
            return;
        }

        var src = new UniTaskCompletionSource<bool>();
        if (!_waiters.TryGetValue(signal, out var list) || list == null)
        {
            list = new LinkedList<UniTaskCompletionSource<bool>>();
            _waiters[signal] = list;
        }
        var node = list.AddLast(src);

        using (token.Register(() =>
        {
            if (node.List != null) node.List.Remove(node);
            src.TrySetCanceled();
        }))
        {
            await src.Task;
        }
    }

    /// <summary>Сбросить только буфер эмитов; активные Wait остаются.</summary>
    public void ClearPending() => _pendingCounts.Clear();
}
