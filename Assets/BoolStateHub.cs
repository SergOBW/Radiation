using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

public class BoolStateHub
{
    private sealed class Entry
    {
        public bool Value;
        public readonly List<UniTaskCompletionSource<bool>> WaitTrue = new List<UniTaskCompletionSource<bool>>();
        public readonly List<UniTaskCompletionSource<bool>> WaitFalse = new List<UniTaskCompletionSource<bool>>();
    }

    private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();

    private Entry GetOrCreate(string key)
    {
        if (!_entries.TryGetValue(key, out var e))
        {
            e = new Entry();
            _entries[key] = e;
        }
        return e;
    }

    public void SetTrue(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        var e = GetOrCreate(key);
        if (e.Value) return;

        e.Value = true;

        // Разбудить ожидающих true
        for (int i = 0; i < e.WaitTrue.Count; i++)
        {
            var src = e.WaitTrue[i];
            if (!src.Task.Status.IsCompleted()) src.TrySetResult(true);
        }
        e.WaitTrue.Clear();
    }

    public void SetFalse(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        var e = GetOrCreate(key);
        if (!e.Value) return;

        e.Value = false;

        // Разбудить ожидающих false
        for (int i = 0; i < e.WaitFalse.Count; i++)
        {
            var src = e.WaitFalse[i];
            if (!src.Task.Status.IsCompleted()) src.TrySetResult(true);
        }
        e.WaitFalse.Clear();
    }

    public bool IsTrue(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        if (!_entries.TryGetValue(key, out var e)) return false;
        return e.Value;
    }

    public async UniTask WaitUntilTrue(string key, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        var e = GetOrCreate(key);

        if (e.Value) return; // уже true

        var src = new UniTaskCompletionSource<bool>();
        e.WaitTrue.Add(src);

        using (token.Register(() =>
        {
            if (!src.Task.Status.IsCompleted()) src.TrySetCanceled();
        }))
        {
            await src.Task;
        }
    }

    public async UniTask WaitUntilFalse(string key, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        var e = GetOrCreate(key);

        if (!e.Value) return; // уже false

        var src = new UniTaskCompletionSource<bool>();
        e.WaitFalse.Add(src);

        using (token.Register(() =>
        {
            if (!src.Task.Status.IsCompleted()) src.TrySetCanceled();
        }))
        {
            await src.Task;
        }
    }

    public void Clear()
    {
        _entries.Clear();
    }
}
