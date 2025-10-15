using UnityEngine;
using VContainer;

public sealed class WaypointMarker : MonoBehaviour
{
    [SerializeField] private string waypointId;

    private WaypointRepository _repository;

    [Inject] public void Construct(WaypointRepository repository)
    {
        _repository = repository;
    }
}