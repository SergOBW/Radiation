using UnityEngine;
using UnityEngine.AI;
using VContainer;

[RequireComponent(typeof(BotController))]
public sealed class BotGazeController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform rotateTarget;
    [SerializeField] private Transform player;
    [SerializeField] private FIMSpace.FLook.FLookAnimator lookAnimator;

    [Inject] private ConversationOrchestrator _orchestrator;

    [Header("Logic")]
    [SerializeField] private bool onlyWhenTalking = true;
    [SerializeField, Range(0f, 180f)] private float bodyTurnOnAngle = 40f;
    [SerializeField, Range(0f, 180f)] private float bodyTurnOffAngle = 25f;

    [Header("Smoothing")]
    [SerializeField, Range(0.01f, 1f)] private float smoothTime = 0.25f;
    [SerializeField, Range(60f, 1080f)] private float maxDegPerSec = 540f;

    private BotController _bot;
    private float _yawVelocity;
    private bool _wasMoving;
    private bool _bodyTurning;

    private void Awake()
    {
        _bot = GetComponent<BotController>();
        if (agent == null) agent = GetComponentInParent<NavMeshAgent>();
        if (rotateTarget == null) rotateTarget = agent != null ? agent.transform : transform;

        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }

        if (lookAnimator == null) lookAnimator = GetComponentInChildren<FIMSpace.FLook.FLookAnimator>();
        if (lookAnimator != null)
        {
            lookAnimator.ObjectToFollow = player;
            lookAnimator.LookAnimatorAmount = 1f;
            lookAnimator.enabled = true;
        }
    }

    private void Update()
    {
        if (_bot == null) return;
        if (agent == null) return;
        if (rotateTarget == null) return;
        if (player == null) return;

        bool isMoving = _bot.IsMoving;

        if (isMoving && !_wasMoving)
        {
            agent.updateRotation = true;
            rotateTarget.rotation = agent.transform.rotation;
        }
        _wasMoving = isMoving;

        if (isMoving)
        {
            DisableLookAnimator();
            return;
        }

        if (onlyWhenTalking)
        {
            if (_orchestrator == null || !_orchestrator.IsRunning)
            {
                DisableLookAnimator();
                return;
            }
        }

        EnableLookAnimator();

        agent.updateRotation = false;

        Vector3 flat = player.position - rotateTarget.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f) return;

        float targetYaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
        float currentYaw = rotateTarget.eulerAngles.y;
        float delta = Mathf.DeltaAngle(currentYaw, targetYaw);

        if (_bodyTurning)
        {
            if (Mathf.Abs(delta) <= bodyTurnOffAngle)
            {
                _bodyTurning = false;
                return;
            }
            RotateBody(currentYaw, targetYaw);
            return;
        }
        if (Mathf.Abs(delta) >= bodyTurnOnAngle)
        {
            _bodyTurning = true;
            RotateBody(currentYaw, targetYaw);
        }
    }

    private void RotateBody(float currentYaw, float targetYaw)
    {
        float newYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref _yawVelocity, smoothTime, maxDegPerSec, Time.deltaTime);
        rotateTarget.rotation = Quaternion.Euler(0f, newYaw, 0f);
    }

    private void DisableLookAnimator()
    {
        if (lookAnimator != null)
        {
            lookAnimator.LookAnimatorAmount = 0f;
            lookAnimator.enabled = false;
        }
    }

    private void EnableLookAnimator()
    {
        if (lookAnimator != null)
        {
            lookAnimator.enabled = true;
            lookAnimator.LookAnimatorAmount = 1f;
            lookAnimator.ObjectToFollow = player;
        }
    }
}
