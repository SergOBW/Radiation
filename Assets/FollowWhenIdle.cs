using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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

    [Header("Update")]
    [SerializeField] private float repathInterval = 0.25f;
    [SerializeField] private float debugLogInterval = 1.0f;

    [Header("Animation (optional)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";

    private NavMeshAgent _agent;
    private ConversationOrchestrator _orchestrator;
    private CancellationTokenSource _cts;
    private float _debugTimer;

    [Inject]
    public void Construct(ConversationOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        _cts = new CancellationTokenSource();
        RunLoop(_cts.Token).Forget();
    }

    private void OnDisable()
    {
        if (_cts != null && !_cts.IsCancellationRequested) _cts.Cancel();
        _cts?.Dispose();
        _cts = null;

        if (_agent != null && _agent.hasPath) _agent.ResetPath();
        if (animator != null && !string.IsNullOrEmpty(speedParam)) animator.SetFloat(speedParam, 0f);
    }

    public void SetTarget(Transform newTarget) => target = newTarget;

    private async UniTaskVoid RunLoop(CancellationToken token)
    {
        if (target == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) target = go.transform;
        }

        while (!token.IsCancellationRequested)
        {
            if (_agent == null || !_agent.isOnNavMesh)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);
                continue;
            }

            bool busy = _orchestrator != null && _orchestrator.IsRunning;
            if (busy || target == null)
            {
                if (_agent.hasPath) _agent.ResetPath();
                if (animator != null && !string.IsNullOrEmpty(speedParam)) animator.SetFloat(speedParam, 0f);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
                continue;
            }

            Vector3 wanted = target.position;
            if (NavMesh.SamplePosition(wanted, out var hit, sampleMaxDistance, NavMesh.AllAreas))
                wanted = hit.position;

            float dist = Vector3.Distance(_agent.transform.position, wanted);

            _agent.stoppingDistance = Mathf.Max(0f, followRadius);

            if (dist > resumeDistance)
            {
                _agent.isStopped = false;
                _agent.SetDestination(wanted);
            }
            else
            {
                if (_agent.hasPath)
                {
                    _agent.isStopped = true;
                    _agent.ResetPath();
                    _agent.isStopped = false;
                }
            }

            if (animator != null && !string.IsNullOrEmpty(speedParam))
                animator.SetFloat(speedParam, _agent.velocity.magnitude);

            if (debugLogInterval > 0f)
            {
                _debugTimer += Time.deltaTime;
                if (_debugTimer >= debugLogInterval)
                {
                    _debugTimer = 0f;
                    Debug.Log($"[FollowWhenIdle:{name}] dist={dist:0.00} rem={_agent.remainingDistance:0.00} vel={_agent.velocity.magnitude:0.00} hasPath={_agent.hasPath} busy={busy}");
                }
            }

            await UniTask.Delay(TimeSpan.FromSeconds(repathInterval), cancellationToken: token);
        }
    }
}
