using UnityEngine;
using UnityEngine.Serialization;
using VContainer;
using VContainer.Unity;

public sealed class RootLifetimeScope : LifetimeScope
{
    [SerializeField] private ConversationScenarioSo scenario;
    [SerializeField] private WaypointRepository waypointRepository;
    [SerializeField] private ActorRepository repository;
    [SerializeField] private StateFlagSpeechReactorRepository stateFlagSpeechReactorRepository;

    [SerializeField] private int startStep = 0;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<BoolStateHub>(Lifetime.Singleton);
        builder.Register<ScenarioSignalHub>(Lifetime.Singleton);
        builder.Register<SceneSignalHub>(Lifetime.Singleton);

        builder.RegisterInstance(waypointRepository);
        builder.RegisterInstance(repository);
        builder.RegisterInstance(stateFlagSpeechReactorRepository);

        builder.RegisterInstance(scenario);
        builder
            .RegisterEntryPoint<ConversationOrchestrator>()
            .WithParameter("startStep", startStep)
            .AsSelf();
    }
}
