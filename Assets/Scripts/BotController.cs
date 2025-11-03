using UnityEngine;
using UnityEngine.AI;
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Threading;
using System;

public sealed class BotController : MonoBehaviour, IBotController
{
    [Header("Components")]
    [SerializeField] private Transform botRoot;
    [SerializeField] private NavMeshAgent navAgent;

    [Header("Options")]
    [SerializeField] private bool stopAgentOnArrive = true;

    [Tooltip("Таймаут репата, если агент потерял путь")]
    [SerializeField] private float repathAfterLostSec = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private float debugTickSeconds = 0.25f;

    [SerializeField] private Color gizmoPathColor = new Color(0.2f, 0.8f, 1f, 0.9f);
    [SerializeField] private Color gizmoTargetColor = new Color(1f, 0.4f, 0.2f, 0.9f);
    [SerializeField] private bool logPathInfo = true;

#if UNITY_EDITOR
    [Header("Editor Debug")]
    [SerializeField] private bool allowDebugStateSwitch = true;
#endif

    private NavMeshPath _lastPath;
    private Vector3 _lastTarget;
    private string _logPrefix;
    private Coroutine _moveCo;
    private bool _moveDone;

    public bool IsMoving { get; private set; }

    public event Action<Vector3, float> OnMoveStarted;
    public event Action<Vector3, Vector3, float> OnMoveStep;
    public event Action<Vector3> OnArrived;
    public event Action OnMoveCancelled;
    public event Action<bool> MovingChanged;

    private void Awake()
    {
        if (navAgent == null) navAgent = GetComponentInChildren<NavMeshAgent>();
        if (botRoot == null) botRoot = transform;
        _lastPath = new NavMeshPath();
        _logPrefix = "[BotController:" + gameObject.name + "] ";

        SetMovingFlag(false);
    }

    public async UniTask MoveToAsync(Vector3 worldPosition, float stopDistance, CancellationToken token)
    {
        if (_moveCo != null) StopCoroutine(_moveCo);

        _moveDone = false;
        SetMovingFlag(true);
        _moveCo = StartCoroutine(MoveRoutine(worldPosition, stopDistance));

        while (!_moveDone && !token.IsCancellationRequested)
            await UniTask.Yield(PlayerLoopTiming.Update, token);

        if (token.IsCancellationRequested && navAgent != null && navAgent.hasPath)
        {
            navAgent.ResetPath();
            SetMovingFlag(false);
            OnMoveCancelled?.Invoke();
        }
    }

    private IEnumerator MoveRoutine(Vector3 worldPosition, float stopDistance)
    {
        if (!navAgent)
        {
            Log("NavMeshAgent is null");
            _moveDone = true;
            SetMovingFlag(false);
            yield break;
        }

        navAgent.updateRotation = true;
        navAgent.autoBraking = true;
        navAgent.autoRepath = false;
        navAgent.isStopped = true;
        navAgent.updatePosition = false;

        if (!navAgent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(botRoot.position, out var selfHit, 2f, NavMesh.AllAreas))
            {
                navAgent.Warp(selfHit.position);
                navAgent.nextPosition = selfHit.position;
            }
            else
            {
                Log("No NavMesh under agent");
                _moveDone = true;
                SetMovingFlag(false);
                yield break;
            }
        }

        if (!NavMesh.SamplePosition(worldPosition, out var targetHit, 8f, NavMesh.AllAreas))
            targetHit.position = worldPosition;

        _lastTarget = targetHit.position;
        navAgent.stoppingDistance = Mathf.Max(0.05f, stopDistance);

        _lastPath.ClearCorners();
        bool ok = navAgent.CalculatePath(_lastTarget, _lastPath);
        if (!ok || _lastPath.status == NavMeshPathStatus.PathInvalid || _lastPath.corners == null || _lastPath.corners.Length < 2)
        {
            Log("Manual path build FAILED");
            _moveDone = true;
            SetMovingFlag(false);
            yield break;
        }

        if (logPathInfo)
        {
            Log("Manual path: corners=" + _lastPath.corners.Length + " len=" + GetPathLength(_lastPath).ToString("0.00") + "m stop=" + navAgent.stoppingDistance.ToString("0.00"));
            for (int i = 0; i < _lastPath.corners.Length; i++) Log("  c[" + i + "]=" + _lastPath.corners[i]);
        }

        OnMoveStarted?.Invoke(_lastTarget, navAgent.stoppingDistance);

        const float cornerReachEps = 0.05f;
        float lastDbg = Time.time;

        int corner = 1;
        Vector3 cur = navAgent.transform.position;

        float speed = Mathf.Max(0.01f, navAgent.speed);
        float angularSpeed = navAgent.angularSpeed;
        float accel = Mathf.Max(0.01f, navAgent.acceleration);

        navAgent.ResetPath();

        while (true)
        {
            Vector3 target = _lastPath.corners[corner];
            Vector3 to = target - cur; to.y = 0f;
            float distToCorner = to.magnitude;

            if (distToCorner <= Mathf.Max(cornerReachEps, navAgent.stoppingDistance))
            {
                corner++;
                if (corner >= _lastPath.corners.Length)
                {
                    float worldDist = Vector3.Distance(cur, _lastTarget);
                    if (worldDist <= navAgent.stoppingDistance + 0.1f)
                    {
                        Log("Arrived(manual). worldDist=" + worldDist.ToString("0.00"));
                        break;
                    }
                    corner = _lastPath.corners.Length - 1;
                    target = _lastTarget;
                    to = target - cur; to.y = 0f; distToCorner = to.magnitude;
                }
                else
                {
                    target = _lastPath.corners[corner];
                    to = target - cur; to.y = 0f; distToCorner = to.magnitude;
                }
            }

            Vector3 dir = (distToCorner > 1e-4f) ? (to / distToCorner) : Vector3.zero;

            if (dir.sqrMagnitude > 1e-6f)
            {
                Quaternion want = Quaternion.LookRotation(dir, Vector3.up);
                navAgent.transform.rotation = Quaternion.RotateTowards(navAgent.transform.rotation, want, angularSpeed * Time.deltaTime);
            }

            speed = Mathf.MoveTowards(speed, navAgent.speed, accel * Time.deltaTime);
            Vector3 step = dir * speed * Time.deltaTime;
            if (step.magnitude > distToCorner) step = dir * distToCorner;

            Vector3 next = cur + step;
            if (NavMesh.SamplePosition(next, out var onMesh, 0.5f, navAgent.areaMask))
            {
                navAgent.Warp(onMesh.position);
                navAgent.nextPosition = onMesh.position;
                cur = onMesh.position;
            }
            else
            {
                _lastPath.ClearCorners();
                bool rebuilt = navAgent.CalculatePath(_lastTarget, _lastPath);
                if (!rebuilt || _lastPath.status == NavMeshPathStatus.PathInvalid || _lastPath.corners.Length < 2)
                {
                    Log("Manual path lost; stop.");
                    break;
                }
                corner = 1;
                continue;
            }

            float remainingToTarget = Vector3.Distance(cur, _lastTarget);
            OnMoveStep?.Invoke(cur, dir, remainingToTarget);

            if (debugMode && Time.time - lastDbg >= debugTickSeconds)
            {
                Log("MoveTick(manual): corner=" + corner + "/" + (_lastPath.corners.Length - 1) + " worldDist=" + remainingToTarget.ToString("0.00") + " step=" + step.magnitude.ToString("0.00"));
                lastDbg = Time.time;
            }

            yield return null;
        }

        if (stopAgentOnArrive)
            navAgent.ResetPath();

        navAgent.updatePosition = true;
        navAgent.isStopped = false;

        OnArrived?.Invoke(_lastTarget);

        _moveDone = true;
        SetMovingFlag(false);
    }

    private void SetMovingFlag(bool value)
    {
        if (IsMoving == value) return;
        IsMoving = value;
        MovingChanged?.Invoke(IsMoving);
    }

#if UNITY_EDITOR
    public void DebugSetMoving(bool value)
    {
        if (!allowDebugStateSwitch) return;
        SetMovingFlag(value);
    }
#endif

    private static float GetPathLength(NavMeshPath path)
    {
        if (path == null || path.corners == null || path.corners.Length < 2) return 0f;
        float len = 0f;
        for (int i = 0; i < path.corners.Length - 1; i++)
            len += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        return len;
    }

    private void Log(string msg)
    {
        if (debugMode) Debug.Log(_logPrefix + msg);
    }

    private void OnDrawGizmos()
    {
        if (!debugMode) return;

        Gizmos.color = gizmoTargetColor;
        Gizmos.DrawSphere(_lastTarget, 0.15f);

        if (_lastPath != null && _lastPath.corners != null && _lastPath.corners.Length > 1)
        {
            Gizmos.color = gizmoPathColor;
            for (int i = 0; i < _lastPath.corners.Length - 1; i++)
                Gizmos.DrawLine(_lastPath.corners[i], _lastPath.corners[i + 1]);
        }
    }
}
