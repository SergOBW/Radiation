using UnityEngine;
using VContainer;

public sealed class WaypointMarker : MonoBehaviour
{
    [SerializeField] private string waypointId;

    private WaypointRegistry _registry;

    [Inject] public void Construct(WaypointRegistry registry) => _registry = registry;

    private void OnEnable()
    {
        if (_registry != null && !string.IsNullOrWhiteSpace(waypointId))
            _registry.Register(waypointId, transform);
    }

    private void OnDisable()
    {
        if (_registry != null && !string.IsNullOrWhiteSpace(waypointId))
            _registry.Unregister(waypointId, transform);
    }
}