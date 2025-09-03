using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;

public sealed class SignalHub
{
    private readonly Dictionary<string, UniTaskCompletionSource<bool>> _sources =
        new Dictionary<string, UniTaskCompletionSource<bool>>();

    public void Emit(string signal)
    {
        if (string.IsNullOrWhiteSpace(signal)) return;

        if (_sources.TryGetValue(signal, out var src))
        {
            if (!src.Task.Status.IsCompleted()) src.TrySetResult(true);
            _sources.Remove(signal);
        }
        else
        {
            var completed = new UniTaskCompletionSource<bool>();
            completed.TrySetResult(true);
            _sources[signal] = completed;
        }
    }

    public async UniTask Wait(string signal, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(signal)) return;

        if (!_sources.TryGetValue(signal, out var src) || src.Task.Status.IsCompleted())
        {
            src = new UniTaskCompletionSource<bool>();
            _sources[signal] = src;
        }

        using (token.Register(() => src.TrySetCanceled()))
        {
            await src.Task;
        }
    }

    public void Clear()
    {
        _sources.Clear();
    }
}