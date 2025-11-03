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
    [SerializeField] private BotController botController;
    [SerializeField] private BotAnimator botAnimator;
    [SerializeField] private NavMeshAgent navAgent;

    [Header("Animator Params")]
    [SerializeField] private string talkingBool = "IsTalking";
    [SerializeField] private string walkingBool = "IsWalking";
    [SerializeField] private string idleBool = "IsIdle";

    [Header("Options")]
    [SerializeField] private bool stopNavMeshWhileTalking = true;

    private CharacterState _currentState;
    private bool _forceTalking;
    private bool _navWasStoppedByTalking;

    public CharacterState CurrentState { get { return _currentState; } }
    public event Action StateChanged;

    private void Awake()
    {
        if (botController == null) botController = GetComponent<BotController>();
        if (botAnimator == null) botAnimator = GetComponent<BotAnimator>();
        if (navAgent == null) navAgent = GetComponentInChildren<NavMeshAgent>();

        ApplyState(CharacterState.Idle);
    }

    private void Update()
    {
        CharacterState next = CalculateNextState();
        if (next != _currentState)
        {
            ApplyState(next);
            StateChanged?.Invoke();
        }
    }

    public void StartTalking()
    {
        _forceTalking = true;
        CharacterState next = CharacterState.Talking;
        if (next != _currentState)
        {
            ApplyState(next);
            StateChanged?.Invoke();
        }
    }

    public void StopTalking()
    {
        _forceTalking = false;
        CharacterState next = CalculateNextState();
        if (next != _currentState)
        {
            ApplyState(next);
            StateChanged?.Invoke();
        }
    }

    public void ToggleTalking()
    {
        if (_forceTalking) StopTalking();
        else StartTalking();
    }

    public void SetTalking(bool value)
    {
        if (value) StartTalking();
        else StopTalking();
    }

    private CharacterState CalculateNextState()
    {
        if (_forceTalking) return CharacterState.Talking;
        if (botController != null && botController.IsMoving) return CharacterState.Walking;
        return CharacterState.Idle;
    }

    private void ApplyState(CharacterState newState)
    {
        _currentState = newState;

        Animator animator = botAnimator != null ? botAnimator.Animator : null;
        if (animator != null)
        {
            if (!string.IsNullOrEmpty(talkingBool)) animator.SetBool(talkingBool, _currentState == CharacterState.Talking);
            if (!string.IsNullOrEmpty(walkingBool)) animator.SetBool(walkingBool, _currentState == CharacterState.Walking);
            if (!string.IsNullOrEmpty(idleBool))    animator.SetBool(idleBool,    _currentState == CharacterState.Idle);
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
            }
        }
        else
        {
            if (_navWasStoppedByTalking)
            {
                navAgent.isStopped = false;
                _navWasStoppedByTalking = false;
            }
        }
    }
}
