using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer.Unity;
public sealed class ConversationContext
{
    public ActorRegistry Registry { get; }
    public SignalHub Signals { get; }
    public WaypointRegistry Waypoints { get; }
    public CancellationToken Token { get; }

    public ConversationContext(ActorRegistry registry, SignalHub signals, WaypointRegistry waypoints, CancellationToken token)
    {
        Registry = registry;
        Signals = signals;
        Waypoints = waypoints;
        Token = token;
    }
}


public sealed class ConversationOrchestrator : IStartable
{
    private readonly ActorRegistry _registry;
    private readonly SignalHub _signals;
    private readonly ConversationScenarioSo _scenario;
    private readonly WaypointRegistry _waypoints;

    private CancellationTokenSource _cts;
    public bool IsRunning { get; private set; }

    public ConversationOrchestrator(ActorRegistry registry,
        SignalHub signals,
        ConversationScenarioSo scenario,
        WaypointRegistry waypoints
        )
    {
        _registry = registry;
        _signals = signals;
        _scenario = scenario;
        _waypoints = waypoints;
    }

    public void Start()
    {
        RunAsync().Forget();
    }

    public async UniTask RunAsync()
    {
        if (IsRunning) return;
        if (_scenario == null || _scenario.steps == null || _scenario.steps.Length == 0) return;

        IsRunning = true;
        _signals.Clear();
        _cts = new CancellationTokenSource();

        ConversationContext ctx = new ConversationContext(_registry, _signals, _waypoints,_cts.Token);

        for (int i = 0; i < _scenario.steps.Length; i++)
        {
            if (_cts.IsCancellationRequested) break;

            ConversationStepSo step = _scenario.steps[i];
            if (step == null) continue;

            try
            {
                await step.Execute(ctx);
            }
            catch (System.OperationCanceledException)
            {
                break;
            }
        }

        _cts.Dispose();
        _cts = null;
        IsRunning = false;
    }

    public void Stop()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
            _cts.Cancel();
    }
}