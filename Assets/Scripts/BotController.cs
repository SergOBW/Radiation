using UnityEngine;
using UnityEngine.AI;
using Cysharp.Threading.Tasks;
using System.Threading;

public sealed class BotController : MonoBehaviour, IBotController
{
    [Header("Components")]
    [SerializeField] private Transform botRoot;
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private Animator animator;

    [Header("Animation")]
    [SerializeField] private string speedParam = "Speed";

    [Header("Options")]
    [SerializeField] private bool stopAgentOnArrive = true;
    [SerializeField] private float stuckSpeedEps = 0.03f;     // ниже — считаем стоим
    [SerializeField] private float stuckCheckSeconds = 1.0f;  // интервал проверки «застряли»

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private Color gizmoPathColor = new Color(0.2f, 0.8f, 1f, 0.9f);
    [SerializeField] private Color gizmoTargetColor = new Color(1f, 0.4f, 0.2f, 0.9f);

    private NavMeshPath _lastPath;        // чтобы рисовать Gizmos
    private Vector3 _lastTarget;          // цель для Gizmos
    private string _logPrefix;

    private void Awake()
    {
        if (navAgent == null) navAgent = GetComponentInChildren<NavMeshAgent>();
        if (botRoot == null) botRoot = transform;
        if (_lastPath == null) _lastPath = new NavMeshPath();
        _logPrefix = $"[BotController:{gameObject.name}] ";
    }

    public async UniTask MoveToAsync(Vector3 worldPosition, float stopDistance, CancellationToken token)
    {
        if (navAgent == null)
        {
            Log("NavMeshAgent is null");
            return;
        }

        _lastTarget = worldPosition;
        navAgent.stoppingDistance = Mathf.Max(0f, stopDistance);

        if (!navAgent.isOnNavMesh)
        {
            Log("Agent is NOT on NavMesh. Trying to sample and warp...");
            if (!TryPlaceOnNavMeshNear(botRoot.position, 2.0f))
            {
                Log("Failed to place agent on NavMesh (SamplePosition failed). Abort.");
                return;
            }
            Log("Warped to nearest NavMesh.");
        }

        // Ставим цель и сразу кэшируем путь (для Gizmos и проверки)
        bool ok = navAgent.SetDestination(worldPosition);
        if (!ok)
        {
            Log($"SetDestination returned false (pos={worldPosition}).");
            return;
        }
        // Для визуализации и дополнительной проверки пробуем построить путь вручную
        NavMesh.CalculatePath(navAgent.transform.position, worldPosition, NavMesh.AllAreas, _lastPath);

        Log($"Destination set. stoppingDistance={navAgent.stoppingDistance:0.00}");

        float stuckTimer = 0f;
        float lastSpeed = 0f;

        while (!token.IsCancellationRequested)
        {
            // Путь ещё строится
            if (navAgent.pathPending)
            {
                LogOncePerFrame("pathPending...");
                await UniTask.Yield(PlayerLoopTiming.Update, token);
                continue;
            }

            // Путь отказан или недоступен
            if (navAgent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                Log("PathInvalid. ResetPath and exit.");
                navAgent.ResetPath();
                break;
            }
            if (navAgent.pathStatus == NavMeshPathStatus.PathPartial)
            {
                Log("PathPartial (дойти не сможем полностью). Продолжаю пока могу.");
            }

            // Синхронизируем анимацию
            if (animator != null && !string.IsNullOrEmpty(speedParam))
                animator.SetFloat(speedParam, navAgent.velocity.magnitude);

            // Условие прибытия
            bool close = navAgent.remainingDistance <= navAgent.stoppingDistance;
            bool stopped = !navAgent.hasPath || navAgent.velocity.sqrMagnitude <= 0.0001f;
            if (close && stopped)
            {
                Log("Arrived.");
                break;
            }

            // Детектор «застряли»: скорость долго ниже порога
            float v = navAgent.velocity.magnitude;
            if (v < stuckSpeedEps)
                stuckTimer += Time.deltaTime;
            else
                stuckTimer = 0f;

            if (stuckTimer >= stuckCheckSeconds && navAgent.hasPath)
            {
                Log($"Seems stuck for {stuckTimer:0.00}s. remaining={navAgent.remainingDistance:0.00}. Try repath.");
                navAgent.SetDestination(worldPosition); // обновим путь
                stuckTimer = 0f;
            }

            // Периодический статус
            if (Mathf.Abs(v - lastSpeed) > 0.05f)
            {
                Log($"rem={navAgent.remainingDistance:0.00} vel={v:0.00} hasPath={navAgent.hasPath} status={navAgent.pathStatus}");
                lastSpeed = v;
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        // Завершение
        if (token.IsCancellationRequested)
        {
            Log("Cancelled. ResetPath.");
            if (navAgent.hasPath) navAgent.ResetPath();
        }
        else
        {
            if (stopAgentOnArrive)
            {
                navAgent.isStopped = true;
                navAgent.ResetPath();
                navAgent.isStopped = false;
            }
        }

        if (animator != null && !string.IsNullOrEmpty(speedParam))
            animator.SetFloat(speedParam, 0f);
    }

    public async UniTask PlayAnimationAsync(string stateName, float normalizedTime, bool waitForExit, CancellationToken token)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            return;

        animator.CrossFadeInFixedTime(stateName, 0.1f, 0, normalizedTime);

        if (!waitForExit) return;

        while (!token.IsCancellationRequested)
        {
            AnimatorStateInfo st = animator.GetCurrentAnimatorStateInfo(0);
            if (st.IsName(stateName) && st.normalizedTime >= 0.99f) break;
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }

    private bool TryPlaceOnNavMeshNear(Vector3 origin, float maxDistance)
    {
        if (NavMesh.SamplePosition(origin, out var hit, maxDistance, NavMesh.AllAreas))
        {
            bool warped = navAgent.Warp(hit.position);
            if (!warped) botRoot.position = hit.position;
            return true;
        }
        return false;
    }

    private int _lastLogFrame = -1;
    private void LogOncePerFrame(string msg)
    {
        if (!debugMode) return;
        if (Time.frameCount == _lastLogFrame) return;
        _lastLogFrame = Time.frameCount;
        Debug.Log(_logPrefix + msg);
    }

    private void Log(string msg)
    {
        if (!debugMode) return;
        Debug.Log(_logPrefix + msg);
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
            {
                Gizmos.DrawLine(_lastPath.corners[i], _lastPath.corners[i + 1]);
            }
        }
    }
}
