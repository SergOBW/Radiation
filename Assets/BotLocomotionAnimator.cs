using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class BotLocomotionAnimator : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private BotAnimator botAnimator;
    [SerializeField] private Transform targetTransform;
    [SerializeField] private BotController botController;
    [SerializeField] private NavMeshAgent navAgent;

    [Header("Animator Params")]
    [SerializeField] private string speedParam = "Speed";

    [Header("Options")]
    [SerializeField] private bool useAgentVelocity = false;
    [SerializeField] private float smoothing = 10f;

#if UNITY_EDITOR
    [Header("Editor Debug")]
    [SerializeField] private bool mockSpeedEnabled = false;
    [SerializeField, Min(0f)] private float mockSpeed = 1.0f;
#endif

    private Vector3 _lastPosition;
    private float _currentSpeed;

    private void Awake()
    {
        if (botAnimator == null) botAnimator = GetComponent<BotAnimator>();
        if (targetTransform == null) targetTransform = transform;
        if (botController == null) botController = GetComponent<BotController>();
        if (navAgent == null) navAgent = GetComponentInChildren<NavMeshAgent>();
        _lastPosition = targetTransform.position;
    }

    private void Update()
    {
        float rawSpeed = 0f;

#if UNITY_EDITOR
        if (mockSpeedEnabled)
        {
            rawSpeed = mockSpeed;
        }
        else
#endif
        {
            if (useAgentVelocity && navAgent != null)
            {
                rawSpeed = navAgent.velocity.magnitude;
            }
            else
            {
                Vector3 current = targetTransform.position;
                rawSpeed = Vector3.Distance(current, _lastPosition) / Mathf.Max(Time.deltaTime, 1e-4f);
                _lastPosition = current;
            }
        }

        _currentSpeed = Mathf.Lerp(_currentSpeed, rawSpeed, 1f - Mathf.Exp(-smoothing * Time.deltaTime));

        Animator animator = botAnimator != null ? botAnimator.Animator : null;
        if (animator != null && !string.IsNullOrEmpty(speedParam))
            animator.SetFloat(speedParam, _currentSpeed);
    }
}
