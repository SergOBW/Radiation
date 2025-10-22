using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public sealed class ConversationContext
{
    public ActorRepository repository { get; }
    public ScenarioSignalHub scenarioSignals { get; }
    public BoolStateHub StateHub { get; }
    public WaypointRepository Waypoints { get; }
    public CancellationToken Token { get; }

    public ConversationContext(ActorRepository repository, ScenarioSignalHub scenarioSignals, BoolStateHub  stateHub,WaypointRepository waypoints, CancellationToken token)
    {
        this.repository = repository;
        this.scenarioSignals = scenarioSignals;
        Waypoints = waypoints;
        Token = token;
        StateHub = stateHub;
    }
}

public sealed class ConversationOrchestrator : IStartable
{
    private readonly ActorRepository _repository;
    public ScenarioSignalHub scenarioSignals => _scenarioSignals;

    private readonly ScenarioSignalHub _scenarioSignals;
    private readonly BoolStateHub _stateHub;

    private readonly ConversationScenarioSo _scenario;
    private readonly WaypointRepository _waypoints;

    private CancellationTokenSource _cts;
    public bool IsRunning { get; private set; }

    private int _stepIndex;

    public ConversationOrchestrator(int startStep,
        ActorRepository repository,
        ScenarioSignalHub scenarioSignals,
        ConversationScenarioSo scenario,
        WaypointRepository waypoints,
        BoolStateHub stateHub)
    {
        _stepIndex = startStep;
        _repository   = repository;
        _scenarioSignals    = scenarioSignals;
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

        _scenarioSignals.Clear();

        _cts = new CancellationTokenSource();
        IsRunning = true;

        var ctx = new ConversationContext(_repository, _scenarioSignals, _stateHub,_waypoints, _cts.Token);

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
