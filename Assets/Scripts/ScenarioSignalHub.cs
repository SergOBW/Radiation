using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;

/// <summary>
/// Хаб для сценария: один эмит = одно потребление.
/// Допускает Emit-до-Wait (буфер-флажок). Clear() безопасно: чистит всё.
/// </summary>
public sealed class ScenarioSignalHub
{
    private readonly Dictionary<string, UniTaskCompletionSource<bool>> _sources =
        new Dictionary<string, UniTaskCompletionSource<bool>>();

    public void Emit(string signal)
    {
        if (string.IsNullOrWhiteSpace(signal)) return;

        if (_sources.TryGetValue(signal, out var src))
        {
            if (!src.Task.Status.IsCompleted())
            {
                src.TrySetResult(true);
                _sources.Remove(signal);
                return;
            }
            // уже лежит completed-флажок — ок, оставляем
            return;
        }

        // Нет ждущего — кладём completed как флажок
        var completed = new UniTaskCompletionSource<bool>();
        completed.TrySetResult(true);
        _sources[signal] = completed;
    }

    public async UniTask Wait(string signal, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(signal)) return;

        if (_sources.TryGetValue(signal, out var existing))
        {
            if (existing.Task.Status.IsCompleted())
            {
                _sources.Remove(signal); // съели флажок
                return;
            }

            using (token.Register(() => existing.TrySetCanceled()))
            {
                await existing.Task;
                _sources.Remove(signal);
                return;
            }
        }

        var src = new UniTaskCompletionSource<bool>();
        _sources[signal] = src;

        using (token.Register(() => src.TrySetCanceled()))
        {
            await src.Task;
            _sources.Remove(signal);
        }
    }

    /// <summary>Полный сброс (разрешён для сценарной логики).</summary>
    public void Clear() => _sources.Clear();
}