using UnityEngine;

public sealed class TableZoneFlagMB : MonoBehaviour ,IStateHubUser
{
    [SerializeField] private string objectId = "Case";
    [SerializeField] private string caseTag = "Case";

    private int _insideCount;
    private BoolStateHub _hub;

    public void SetStateHub(BoolStateHub hub) { _hub = hub; }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(caseTag)) return;
        _insideCount++;
        if (_hub == null) return;
        _hub.SetTrue("InZone:" + objectId + "OnTable");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(caseTag)) return;
        _insideCount = Mathf.Max(0, _insideCount - 1);
        if (_insideCount > 0) return;
        if (_hub == null) return;
        _hub.SetFalse("InZone:" + objectId + "OnTable");
    }
}