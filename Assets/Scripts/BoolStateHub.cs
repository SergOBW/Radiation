using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class BoolStateHub
{
    private sealed class StateEntry
    {
        public bool IsTrue;
        public readonly List<UniTaskCompletionSource<bool>> WaitTrueSources = new List<UniTaskCompletionSource<bool>>();
        public readonly List<UniTaskCompletionSource<bool>> WaitFalseSources = new List<UniTaskCompletionSource<bool>>();
    }

    private readonly Dictionary<string, StateEntry> _entries = new Dictionary<string, StateEntry>();

    private StateEntry GetOrCreateEntry(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        if (!_entries.TryGetValue(key, out StateEntry entry))
        {
            entry = new StateEntry();
            _entries[key] = entry;
            Debug.Log($"[BoolStateHub] Создан новый ключ '{key}'.");
        }

        return entry;
    }

    public void SetTrue(string key)
    {
        var entry = GetOrCreateEntry(key);
        if (entry == null) return;
        if (entry.IsTrue) return;

        entry.IsTrue = true;
        Debug.Log($"[BoolStateHub] SetTrue('{key}').");

        for (int i = 0; i < entry.WaitTrueSources.Count; i++)
        {
            var source = entry.WaitTrueSources[i];
            source.TrySetResult(true);
        }

        if (entry.WaitTrueSources.Count > 0)
            Debug.Log($"[BoolStateHub] Пробуждено {entry.WaitTrueSources.Count} ожидающих WaitUntilTrue('{key}').");

        entry.WaitTrueSources.Clear();
    }

    public void SetFalse(string key)
    {
        var entry = GetOrCreateEntry(key);
        if (entry == null) return;
        if (!entry.IsTrue) return;

        entry.IsTrue = false;
        Debug.Log($"[BoolStateHub] SetFalse('{key}').");

        for (int i = 0; i < entry.WaitFalseSources.Count; i++)
        {
            var source = entry.WaitFalseSources[i];
            source.TrySetResult(true);
        }

        if (entry.WaitFalseSources.Count > 0)
            Debug.Log($"[BoolStateHub] Пробуждено {entry.WaitFalseSources.Count} ожидающих WaitUntilFalse('{key}').");

        entry.WaitFalseSources.Clear();
    }

    public bool IsTrue(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        if (!_entries.TryGetValue(key, out StateEntry entry)) return false;
        return entry.IsTrue;
    }

    public async UniTask WaitUntilTrue(string key, CancellationToken cancellationToken)
    {
        var entry = GetOrCreateEntry(key);
        if (entry == null) return;

        if (entry.IsTrue)
        {
            Debug.Log($"[BoolStateHub] WaitUntilTrue('{key}') уже true — выход.");
            return;
        }

        Debug.Log($"[BoolStateHub] WaitUntilTrue('{key}') — ожидание...");
        var source = new UniTaskCompletionSource<bool>();
        entry.WaitTrueSources.Add(source);

        var registration = cancellationToken.Register(CancelWaiter, source);
        try
        {
            await source.Task;
            Debug.Log($"[BoolStateHub] WaitUntilTrue('{key}') завершено.");
        }
        finally
        {
            registration.Dispose();
        }
    }

    public async UniTask WaitUntilFalse(string key, CancellationToken cancellationToken)
    {
        var entry = GetOrCreateEntry(key);
        if (entry == null) return;

        if (!entry.IsTrue)
        {
            Debug.Log($"[BoolStateHub] WaitUntilFalse('{key}') уже false — выход.");
            return;
        }

        Debug.Log($"[BoolStateHub] WaitUntilFalse('{key}') — ожидание...");
        var source = new UniTaskCompletionSource<bool>();
        entry.WaitFalseSources.Add(source);

        var registration = cancellationToken.Register(CancelWaiter, source);
        try
        {
            await source.Task;
            Debug.Log($"[BoolStateHub] WaitUntilFalse('{key}') завершено.");
        }
        finally
        {
            registration.Dispose();
        }
    }

    public void Clear()
    {
        _entries.Clear();
        Debug.Log("[BoolStateHub] Очистка всех состояний.");
    }

    private static void CancelWaiter(object state)
    {
        var source = state as UniTaskCompletionSource<bool>;
        if (source == null) return;
        source.TrySetCanceled();
        Debug.Log("[BoolStateHub] Ожидание отменено через токен.");
    }

    public void Set(string key, bool flag)
    {
        if (flag)
        {
            SetTrue(key);
        }
        else
        {
            SetFalse(key);
        }

    }
}
