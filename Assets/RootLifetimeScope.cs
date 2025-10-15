using UnityEngine;
using UnityEngine.Serialization;
using VContainer;
using VContainer.Unity;

public sealed class RootLifetimeScope : LifetimeScope
{
    [SerializeField] private ConversationScenarioSo scenario;
    [SerializeField] private WaypointRepository waypointRepository;

    [SerializeField] private int startStep = 0;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<ActorRegistry>(Lifetime.Singleton);
        builder.Register<BoolStateHub>(Lifetime.Singleton);
        builder.Register<SignalHub>(Lifetime.Singleton);
        builder.RegisterInstance(waypointRepository);

        builder.RegisterInstance(scenario);
        builder
            .RegisterEntryPoint<ConversationOrchestrator>()
            .WithParameter("startStep", startStep)
            .AsSelf();
    }
}
