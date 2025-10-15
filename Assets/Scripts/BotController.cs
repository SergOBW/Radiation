using UnityEngine;
using UnityEngine.AI;
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Threading;

public sealed class BotController : MonoBehaviour, IBotController
{
    [Header("Components")]
    [SerializeField] private Transform botRoot;
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private Animator animator;

    [Header("Animation")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private bool disableRootMotion = true;

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

    private NavMeshPath _lastPath;
    private Vector3 _lastTarget;
    private string _logPrefix;
    private Coroutine _moveCo;
    private bool _moveDone;

    private void Awake()
    {
        if (navAgent == null) navAgent = GetComponentInChildren<NavMeshAgent>();
        if (botRoot == null) botRoot = transform;
        _lastPath = new NavMeshPath();
        _logPrefix = $"[BotController:{gameObject.name}] ";

        if (animator != null && disableRootMotion)
            animator.applyRootMotion = false;
    }

    // ВНЕШНИЙ API НЕ МЕНЯЛ: ждём пока корутина выставит _moveDone
    public async UniTask MoveToAsync(Vector3 worldPosition, float stopDistance, CancellationToken token)
    {
        if (_moveCo != null) StopCoroutine(_moveCo);
        _moveDone = false;
        _moveCo = StartCoroutine(MoveRoutine(worldPosition, stopDistance));

        while (!_moveDone && !token.IsCancellationRequested)
            await UniTask.Yield(PlayerLoopTiming.Update, token);

        if (token.IsCancellationRequested && navAgent != null && navAgent.hasPath)
            navAgent.ResetPath();
    }

    public async UniTask PlayAnimationAsync(string stateName, float normalizedTime, bool waitForExit, CancellationToken token)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            return;

        // Мягкий старт анимации
        animator.CrossFadeInFixedTime(stateName, 0.1f, 0, Mathf.Clamp01(normalizedTime));

        if (!waitForExit)
            return;

        // Ждём пока клип почти проиграется
        while (!token.IsCancellationRequested)
        {
            var st = animator.GetCurrentAnimatorStateInfo(0);
            if (st.IsName(stateName) && st.normalizedTime >= 0.99f)
                break;

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }


private IEnumerator MoveRoutine(Vector3 worldPosition, float stopDistance)
{
    if (!navAgent) { Log("NavMeshAgent is null"); _moveDone = true; yield break; }

    // Базовые настройки
    navAgent.updateRotation = true;
    navAgent.autoBraking    = true;
    navAgent.autoRepath     = false;   // в ручном режиме не нужен
    navAgent.isStopped      = true;    // сами двигаем
    navAgent.updatePosition = false;   // позицию обновляем вручную через Move/Warp

    // 1) Агент на сетке
    if (!navAgent.isOnNavMesh)
    {
        if (NavMesh.SamplePosition(botRoot.position, out var selfHit, 2f, NavMesh.AllAreas))
        {
            navAgent.Warp(selfHit.position);
            navAgent.nextPosition = selfHit.position;
        }
        else { Log("No NavMesh under agent"); _moveDone = true; yield break; }
    }

    // 2) Цель → ближайшая валидная точка
    if (!NavMesh.SamplePosition(worldPosition, out var targetHit, 8f, NavMesh.AllAreas))
        targetHit.position = worldPosition;

    _lastTarget = targetHit.position;
    navAgent.stoppingDistance = Mathf.Max(0.05f, stopDistance);

    // 3) Строим путь ЧЕРЕЗ АГЕНТА (учитывает agentType/areaMask)
    _lastPath.ClearCorners();
    bool ok = navAgent.CalculatePath(_lastTarget, _lastPath);
    if (!ok || _lastPath.status == NavMeshPathStatus.PathInvalid || _lastPath.corners == null || _lastPath.corners.Length < 2)
    {
        Log($"Manual path build FAILED (mask={navAgent.areaMask})");
        _moveDone = true;
        yield break;
    }

    if (logPathInfo)
    {
        Log($"Manual path: corners={_lastPath.corners.Length} len={GetPathLength(_lastPath):0.00}m stop={navAgent.stoppingDistance:0.00}");
        for (int i = 0; i < _lastPath.corners.Length; i++) Log($"  c[{i}]={_lastPath.corners[i]}");
    }

    // 4) Ручное следование по углам
    const float cornerReachEps = 0.05f;
    const float stopVelEps     = 0.05f;
    float       lastDbg        = Time.time;

    int corner = 1; // 0 — текущая позиция, стартуем с 1-го угла
    Vector3 cur = navAgent.transform.position;

    // для плавности поворота
    float speed        = Mathf.Max(0.01f, navAgent.speed);
    float angularSpeed = navAgent.angularSpeed; // град/сек
    float accel        = Mathf.Max(0.01f, navAgent.acceleration);

    // обнулим любые внутренние пути
    navAgent.ResetPath();

    while (true)
    {
        // цель текущего сегмента
        Vector3 target = _lastPath.corners[corner];
        Vector3 to     = (target - cur);
        to.y = 0f;
        float distToCorner = to.magnitude;

        // достигли текущего угла?
        if (distToCorner <= Mathf.Max(cornerReachEps, navAgent.stoppingDistance))
        {
            corner++;
            if (corner >= _lastPath.corners.Length)
            {
                // финальная проверка на точку прибытия
                float worldDist = Vector3.Distance(cur, _lastTarget);
                if (worldDist <= navAgent.stoppingDistance + 0.1f)
                {
                    Log($"Arrived(manual). worldDist={worldDist:0.00}");
                    break;
                }
                // на всякий случай — дёрнем финальную подстройку
                corner = _lastPath.corners.Length - 1;
                target = _lastTarget;
                to     = (target - cur); to.y = 0f; distToCorner = to.magnitude;
            }
            else
            {
                // следующий угол
                target = _lastPath.corners[corner];
                to     = (target - cur); to.y = 0f; distToCorner = to.magnitude;
            }
        }

        // направление и скорость
        Vector3 dir = (distToCorner > 1e-4f) ? (to / distToCorner) : Vector3.zero;

        // плавный поворот
        if (dir.sqrMagnitude > 1e-6f)
        {
            Quaternion want = Quaternion.LookRotation(dir, Vector3.up);
            navAgent.transform.rotation = Quaternion.RotateTowards(navAgent.transform.rotation, want, angularSpeed * Time.deltaTime);
        }

        // разгон/торможение и перемещение
        speed = Mathf.MoveTowards(speed, navAgent.speed, accel * Time.deltaTime);
        Vector3 step = dir * speed * Time.deltaTime;

        // чтобы не перелетать угол
        if (step.magnitude > distToCorner) step = dir * distToCorner;

        // пробуем прилипнуть к сетке по месту шага (подстраховка)
        Vector3 next = cur + step;
        if (NavMesh.SamplePosition(next, out var onMesh, 0.5f, navAgent.areaMask))
        {
            navAgent.Warp(onMesh.position);          // безопасно «кладём» на сетку
            navAgent.nextPosition = onMesh.position; // держим синхронизировано
            cur = onMesh.position;
        }
        else
        {
            // если не нашли сетку рядом — пересчитаем путь от текущей позиции
            _lastPath.ClearCorners();
            bool rebuilt = navAgent.CalculatePath(_lastTarget, _lastPath);
            if (!rebuilt || _lastPath.status == NavMeshPathStatus.PathInvalid || _lastPath.corners.Length < 2)
            {
                Log("Manual path lost; stop.");
                break;
            }
            corner = 1; // заново идём
            continue;
        }

        // анимация
        if (animator && !string.IsNullOrEmpty(speedParam))
            animator.SetFloat(speedParam, step.magnitude / Mathf.Max(Time.deltaTime, 1e-4f));

        // телеметрия
        if (debugMode && Time.time - lastDbg >= debugTickSeconds)
        {
            float worldDist = Vector3.Distance(cur, _lastTarget);
            Log($"MoveTick(manual): corner={corner}/{_lastPath.corners.Length-1} worldDist={worldDist:0.00} step={step.magnitude:0.00}");
            lastDbg = Time.time;
        }

        yield return null;
    }

    // Завершение
    if (stopAgentOnArrive)
    {
        // очистим любые внутренние пути и вернём управление агенту
        navAgent.ResetPath();
    }

    // вернём стандартное поведение обновления позиции (если нужно)
    navAgent.updatePosition = true;
    navAgent.isStopped = false;

    if (animator && !string.IsNullOrEmpty(speedParam))
        animator.SetFloat(speedParam, 0f);

    _moveDone = true;
}



    // --- utils ---
    private static float GetPathLength(NavMeshPath path)
    {
        if (path == null || path.corners == null || path.corners.Length < 2) return 0f;
        float len = 0f;
        for (int i = 0; i < path.corners.Length - 1; i++)
            len += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        return len;
    }

    private string Fmt(float v) => (v == Mathf.Infinity) ? "Inf" : v.ToString("0.00");
    private void Log(string msg) { if (debugMode) Debug.Log(_logPrefix + msg); }

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
