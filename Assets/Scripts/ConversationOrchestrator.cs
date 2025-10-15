using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public sealed class ConversationContext
{
    public ActorRegistry Registry { get; }
    public SignalHub Signals { get; }
    public BoolStateHub StateHub { get; }
    public WaypointRepository Waypoints { get; }
    public CancellationToken Token { get; }

    public ConversationContext(ActorRegistry registry, SignalHub signals, BoolStateHub  stateHub,WaypointRepository waypoints, CancellationToken token)
    {
        Registry = registry;
        Signals = signals;
        Waypoints = waypoints;
        Token = token;
        StateHub = stateHub;
    }
}

public sealed class ConversationOrchestrator : IStartable
{
    private readonly ActorRegistry _registry;
    public SignalHub Signals => _signals;

    private readonly SignalHub _signals;
    private readonly BoolStateHub _stateHub;

    private readonly ConversationScenarioSo _scenario;
    private readonly WaypointRepository _waypoints;

    private CancellationTokenSource _cts;
    public bool IsRunning { get; private set; }

    private int _stepIndex;

    public ConversationOrchestrator(int startStep,
        ActorRegistry registry,
        SignalHub signals,
        ConversationScenarioSo scenario,
        WaypointRepository waypoints,
        BoolStateHub stateHub)
    {
        _stepIndex = startStep;
        _registry   = registry;
        _signals    = signals;
        _scenario   = scenario;
        _waypoints  = waypoints;
        _stateHub = stateHub;
    }

    public void Start()
    {
        Restart().Forget();
    }

    public async UniTask Restart()
    {
        Stop();

        await UniTask.Yield();

        await RunAsync();
    }

    public async UniTask RunAsync()
    {
        if (_scenario == null || _scenario.steps == null || _scenario.steps.Length == 0) return;
        if (IsRunning) return;

        _signals.Clear();

        _cts = new CancellationTokenSource();
        IsRunning = true;

        var ctx = new ConversationContext(_registry, _signals, _stateHub,_waypoints, _cts.Token);

        try
        {
            for (; _stepIndex < _scenario.steps.Length; _stepIndex++)
            {
                if (_cts.IsCancellationRequested) break;

                var step = _scenario.steps[_stepIndex];
                if (step == null) continue;

                UnityEngine.Debug.Log($"[Conversation] Step {_stepIndex}/{_scenario.steps.Length - 1}: {step.name}");

                await step.Execute(ctx);
            }
        }
        catch (System.OperationCanceledException)
        {

        }
        finally
        {
            IsRunning = false;

            _cts?.Dispose();
            _cts = null;

            _stepIndex = 0;
        }
    }

    public void Stop()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
            _cts.Cancel();
    }
}
