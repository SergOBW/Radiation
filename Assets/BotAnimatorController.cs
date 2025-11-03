using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BotAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float crossFadeTime = 0.1f;

    public Animator Animator { get { return animator; } }

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    public async UniTask PlayAnimationAsync(string stateName, float normalizedTime, bool waitForExit, CancellationToken token)
    {
        if (animator == null) return;
        if (string.IsNullOrWhiteSpace(stateName)) return;

        float clampedTime = Mathf.Clamp01(normalizedTime);
        animator.CrossFadeInFixedTime(stateName, crossFadeTime, 0, clampedTime);

        if (!waitForExit) return;

        while (!token.IsCancellationRequested)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(stateName) && info.normalizedTime >= 0.99f) break;
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }
}