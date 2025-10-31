using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using VContainer;

[RequireComponent(typeof(NavMeshAgent))]
public sealed class FollowWhenIdle : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Distances")]
    [SerializeField] private float followRadius = 1.8f;
    [SerializeField] private float resumeDistance = 2.5f;
    [SerializeField] private float sampleMaxDistance = 1.0f;

    [Header("Update/Pathing")]
    [SerializeField] private float repathInterval = 0.25f;
    [SerializeField] private float stillSpeedThreshold = 0.05f;

    [Header("Forbid walking (state flags)")]
    [Tooltip("ЕСЛИ ХОТЯ БЫ ОДИН из этих флагов = FALSE → ХОДЬБА ЗАПРЕЩЕНА (все должны быть TRUE).")]
    [SerializeField] private string[] forbidStates = { "Talking", "Frozen", "NoFollow" };

    [Header("Animation (optional)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private float debugTick = 0.5f;
    private float _dbgTimer;

    private NavMeshAgent _agent;

    [SerializeField] private BotController botController;
    private BoolStateHub _stateHub;

    private float _nextRepathTime;

    [Inject]
    public void Construct(BoolStateHub stateHub)
    {
        _stateHub = stateHub;
    }

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        if (target == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) target = go.transform;
        }

        _nextRepathTime = Time.time;
        if (_agent != null && _agent.hasPath) _agent.ResetPath();
        SetAnimSpeed(0f);
    }

    private void OnDisable()
    {
        if (_agent != null && _agent.hasPath) _agent.ResetPath();
        SetAnimSpeed(0f);
    }

    private void Update()
    {
        if (_agent == null) { Dbg("No NavMeshAgent"); return; }
        if (!_agent.isOnNavMesh) { Dbg("Agent not on NavMesh"); return; }

        // 1) Проверяем запреты (BotController + флаги)
        if (IsForbidden(out string reason))
        {
            StopIfHasPath();
            SetAnimSpeed(_agent.velocity.magnitude);
            Dbg($"STOP (forbidden): {reason}");
            return;
        }

        if (target == null)
        {
            StopIfHasPath();
            SetAnimSpeed(_agent.velocity.magnitude);
            Dbg("STOP: target == null");
            return;
        }

        // 2) Следуем
        _agent.stoppingDistance = Mathf.Max(0f, followRadius);

        Vector3 wanted = target.position;
        if (NavMesh.SamplePosition(wanted, out var hit, sampleMaxDistance, NavMesh.AllAreas))
            wanted = hit.position;

        float dist = Vector3.Distance(_agent.transform.position, wanted);

        if (dist > resumeDistance)
        {
            if (Time.time >= _nextRepathTime)
            {
                _nextRepathTime = Time.time + repathInterval;
                _agent.isStopped = false;
                bool ok = _agent.SetDestination(wanted);
                Dbg($"GO: dist={dist:0.00}, setDest={ok}, wanted=({wanted.x:0.00},{wanted.y:0.00},{wanted.z:0.00})");
            }
            else
            {
                Dbg($"WAIT (repath throttle): dist={dist:0.00}, next in {(_nextRepathTime - Time.time):0.00}s");
            }
        }
        else
        {
            StopIfHasPath();
            Dbg($"IDLE: dist={dist:0.00} ≤ resume={resumeDistance:0.00}");
        }

        SetAnimSpeed(_agent.velocity.magnitude);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        _nextRepathTime = Time.time; // мгновенный репас
        Dbg($"New target: {target?.name ?? "null"}");
    }

    /// <summary>
    /// Ходьба запрещена если:
    /// - botController.IsMoving == TRUE (бот уже двигается по своим задачам)
    /// - StateHub отсутствует
    /// - Любой флаг из forbidStates == FALSE
    /// </summary>
    private bool IsForbidden(out string reason)
    {
        if (botController != null && botController.IsMoving)
        {
            reason = "BotController.IsMoving = TRUE";
            return true;
        }

        if (_stateHub == null)
        {
            reason = "StateHub == null";
            return true; // безопаснее запретить, чтобы не получить гонки
        }

        if (forbidStates != null && forbidStates.Length > 0)
        {
            foreach (var flag in forbidStates)
            {
                if (string.IsNullOrWhiteSpace(flag)) continue;
                bool v = _stateHub.IsTrue(flag);
                if (!v)
                {
                    reason = $"flag '{flag}' = FALSE";
                    return true;
                }
            }
        }

        reason = "all flags TRUE, bot idle";
        return false;
    }

    private void StopIfHasPath()
    {
        if (_agent.hasPath)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.isStopped = false;
        }
    }

    private void SetAnimSpeed(float speed)
    {
        if (animator != null && !string.IsNullOrEmpty(speedParam))
            animator.SetFloat(speedParam, speed);
    }

    private void Dbg(string msg)
    {
        if (!debugMode) return;
        _dbgTimer += Time.deltaTime;
        if (_dbgTimer >= debugTick)
        {
            _dbgTimer = 0f;
            Debug.Log($"[FollowWhenIdle:{name}] {msg}");
        }
    }
}
