using UnityEngine;
using UnityEngine.AI;
using System;

public enum CharacterState
{
    Idle = 0,
    Walking = 1,
    Talking = 2
}

[DisallowMultipleComponent]
public sealed class CharacterStateController : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private BotAnimator botAnimator;
    [SerializeField] private NavMeshAgent navAgent;

    [Header("Animator Params")]
    [SerializeField] private string talkingBool = "IsTalking";
    [SerializeField] private string walkingBool = "IsWalking";
    [SerializeField] private string idleBool = "IsIdle";

    [Header("Options")]
    [SerializeField] private bool stopNavMeshWhileTalking = true;
    [SerializeField] private float movingSpeedThreshold = 0.05f;
    [SerializeField] private float transformMoveThreshold = 0.01f; // для ручного Warp-движения

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private float debugTickSeconds = 0.25f;

    private CharacterState _currentState;
    private bool _forceTalking;
    private bool _navWasStoppedByTalking;

    private Vector3 _lastWorldPosition;
    private float _nextDbgTime;

    public CharacterState CurrentState { get { return _currentState; } }
    public event Action StateChanged;

    private void Awake()
    {
        if (botAnimator == null) botAnimator = GetComponent<BotAnimator>();
        if (navAgent == null) navAgent = GetComponentInChildren<NavMeshAgent>();
        _lastWorldPosition = transform.position;
        Dbg("Awake: navAgent=" + (navAgent != null));
        ApplyState(CharacterState.Idle);
    }

    private void Update()
    {
        CharacterState nextState = CalculateNextState();
        if (nextState != _currentState)
        {
            Dbg("State change: " + _currentState + " -> " + nextState);
            ApplyState(nextState);
            StateChanged?.Invoke();
        }

        _lastWorldPosition = transform.position;
    }

    public void StartTalking()
    {
        _forceTalking = true;
        Dbg("StartTalking");
        if (_currentState != CharacterState.Talking)
        {
            ApplyState(CharacterState.Talking);
            StateChanged?.Invoke();
        }
    }

    public void StopTalking()
    {
        _forceTalking = false;
        Dbg("StopTalking");
        CharacterState nextState = CalculateNextState();
        if (nextState != _currentState)
        {
            ApplyState(nextState);
            StateChanged?.Invoke();
        }
    }

    public void ToggleTalking()
    {
        Dbg("ToggleTalking before: forceTalking=" + _forceTalking);
        if (_forceTalking) StopTalking();
        else StartTalking();
    }

    public void SetTalking(bool value)
    {
        Dbg("SetTalking=" + value);
        if (value) StartTalking();
        else StopTalking();
    }

    private CharacterState CalculateNextState()
    {
        if (_forceTalking)
        {
            PeriodicDbg("Calc: forced Talking");
            return CharacterState.Talking;
        }

        bool moving = IsMovingNow(out string reason);
        if (moving)
        {
            PeriodicDbg("Calc: moving=true (" + reason + ")");
            return CharacterState.Walking;
        }

        PeriodicDbg("Calc: moving=false");
        return CharacterState.Idle;
    }

    private bool IsMovingNow(out string reason)
    {
        reason = "no agent";
        if (navAgent == null)
        {
            return TransformMoved(out reason);
        }

        bool onMesh = navAgent.isOnNavMesh;
        bool stopped = navAgent.isStopped;
        bool updatesPosition = navAgent.updatePosition;
        bool hasPath = navAgent.hasPath;
        float speed = navAgent.velocity.magnitude;
        float remain = navAgent.remainingDistance;
        float stopDist = navAgent.stoppingDistance;

        // 1) Обычный режим агента
        if (updatesPosition && !stopped && onMesh)
        {
            if (speed > movingSpeedThreshold)
            {
                reason = "speed=" + speed.ToString("0.000") + " > thr";
                PeriodicDbg("Move(agent): on=" + onMesh + " stop=" + stopped + " updPos=" + updatesPosition + " speed=" + speed.ToString("0.000"));
                return true;
            }

            if (hasPath && remain > stopDist + 0.02f)
            {
                reason = "hasPath remain=" + remain.ToString("0.00");
                PeriodicDbg("Move(agent): path remain=" + remain.ToString("0.00"));
                return true;
            }
        }

        // 2) Ручное перемещение (Warp/nextPosition, updatePosition=false)
        if (!updatesPosition || stopped || !onMesh || speed <= movingSpeedThreshold)
        {
            bool movedByTransform = TransformMoved(out string tReason);
            if (movedByTransform)
            {
                reason = "manual " + tReason;
                PeriodicDbg("Move(manual): " + reason + " | on=" + onMesh + " stop=" + stopped + " updPos=" + updatesPosition + " speed=" + speed.ToString("0.000"));
                return true;
            }
        }

        reason = "idle (speed=" + speed.ToString("0.000") + ", path=" + hasPath + ", remain=" + remain.ToString("0.00") + ")";
        PeriodicDbg("Move(idle): on=" + onMesh + " stop=" + stopped + " updPos=" + updatesPosition + " speed=" + speed.ToString("0.000"));
        return false;
    }

    private bool TransformMoved(out string reason)
    {
        Vector3 now = transform.position;
        float dist = (now - _lastWorldPosition).magnitude;
        if (dist > transformMoveThreshold)
        {
            reason = "transformΔ=" + dist.ToString("0.000") + " > thr=" + transformMoveThreshold.ToString("0.000");
            return true;
        }
        reason = "transformΔ=" + dist.ToString("0.000") + " ≤ thr";
        return false;
    }

    private void ApplyState(CharacterState newState)
    {
        _currentState = newState;

        Animator animator = botAnimator != null ? botAnimator.Animator : null;
        if (animator != null)
        {
            bool talk = _currentState == CharacterState.Talking;
            bool walk = _currentState == CharacterState.Walking;
            bool idle = _currentState == CharacterState.Idle;

            if (!string.IsNullOrEmpty(talkingBool)) animator.SetBool(talkingBool, talk);
            if (!string.IsNullOrEmpty(walkingBool)) animator.SetBool(walkingBool, walk);
            if (!string.IsNullOrEmpty(idleBool))    animator.SetBool(idleBool,    idle);

            bool useRoot = idle || talk;
            animator.applyRootMotion = useRoot;

            Dbg("ApplyState: " + _currentState + " | talk=" + talk + " walk=" + walk + " idle=" + idle + " root=" + useRoot);
        }
        else
        {
            Dbg("ApplyState: " + _currentState + " | animator=null");
        }

        HandleNavMeshOnTalking(_currentState == CharacterState.Talking);
    }

    private void HandleNavMeshOnTalking(bool talkingActive)
    {
        if (navAgent == null) return;
        if (!stopNavMeshWhileTalking) return;

        if (talkingActive)
        {
            if (!navAgent.isStopped)
            {
                navAgent.isStopped = true;
                _navWasStoppedByTalking = true;
                Dbg("NavMesh: stop by Talking");
            }
        }
        else
        {
            if (_navWasStoppedByTalking)
            {
                navAgent.isStopped = false;
                _navWasStoppedByTalking = false;
                Dbg("NavMesh: resume after Talking");
            }
        }
    }

    private void Dbg(string msg)
    {
        if (!debugMode) return;
        Debug.Log("[CharacterStateController:" + name + "] " + msg);
    }

    private void PeriodicDbg(string msg)
    {
        if (!debugMode) return;
        if (Time.unscaledTime < _nextDbgTime) return;
        _nextDbgTime = Time.unscaledTime + debugTickSeconds;
        Debug.Log("[CharacterStateController:" + name + "] " + msg);
    }
}
