using UnityEngine;

public sealed class ActorBinding : MonoBehaviour
{
    [SerializeField] private string actorId;
    [SerializeField] private SpeechService speechService;
    [SerializeField] private Component botComponent; // должен реализовывать IBotController
    [SerializeField] private Transform rootTransform;

    public string ActorId => actorId;
    public SpeechService Speech => speechService;
    public IBotController Bot => botComponent as IBotController;
    public Transform Root => rootTransform != null ? rootTransform : transform;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(actorId))
            actorId = gameObject.name;

        if (botComponent != null && !(botComponent is IBotController))
            Debug.LogWarning($"[ActorBinding] botComponent у '{actorId}' не реализует IBotController.", botComponent);

        if (rootTransform == null)
            rootTransform = transform;
    }
#endif
}