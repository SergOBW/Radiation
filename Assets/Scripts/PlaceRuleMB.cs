using UnityEngine;

public sealed class PlaceRuleMB : MonoBehaviour, IStateHubUser
{
    [SerializeField] private string objectId = "Case";

    private BoolStateHub _hub;
    private bool _lastInZone;
    private bool _lastHeld;

    public void SetStateHub(BoolStateHub hub) { _hub = hub; }

    private void Update()
    {
        if (_hub == null) return;

        bool inZone = _hub.IsTrue("InZone:" + objectId + "OnTable");
        bool held = _hub.IsTrue("Held:" + objectId);

        if (inZone != _lastInZone || held != _lastHeld)
        {
            bool placed = inZone && !held;
            if (placed) _hub.SetTrue("Placed:" + objectId + "OnTable");
            else _hub.SetFalse("Placed:" + objectId + "OnTable");
            _lastInZone = inZone;
            _lastHeld = held;
        }
    }
}