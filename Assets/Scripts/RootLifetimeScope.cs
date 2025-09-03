using UnityEngine;
using VContainer;
using VContainer.Unity;

public sealed class RootLifetimeScope : LifetimeScope
{
    [SerializeField] private ConversationScenarioSo scenario;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<ActorRegistry>(Lifetime.Singleton);
        builder.Register<SignalHub>(Lifetime.Singleton);
        builder.Register<WaypointRegistry>(Lifetime.Singleton);

        builder.RegisterInstance(scenario);
        builder.RegisterEntryPoint<ConversationOrchestrator>().AsSelf();
    }
}
