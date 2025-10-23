using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VContainer;

[RequireComponent(typeof(XRGrabInteractable))]
public sealed class VRHoldFlagEmitter : MonoBehaviour, IStateHubUser
{
    [SerializeField] private string holdKeyPrefix = "Held:"; // Итоговый ключ = prefix + id
    [SerializeField] private string objectId = "Default";     // Подкинь из инспектора или сгенерируй


    private BoolStateHub _stateHub;

    private XRGrabInteractable _grab;

    private string key => holdKeyPrefix + objectId;

    public void SetStateHub(BoolStateHub stateHub)
    {
        _stateHub = stateHub;
    }

    private void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        if (_grab != null)
        {
            _grab.selectEntered.AddListener(OnGrab);
            _grab.selectExited.AddListener(OnRelease);
        }
    }

    private void OnDisable()
    {
        if (_grab != null)
        {
            _grab.selectEntered.RemoveListener(OnGrab);
            _grab.selectExited.RemoveListener(OnRelease);
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        _stateHub.SetTrue(key);
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        _stateHub.SetFalse(key);
    }
}