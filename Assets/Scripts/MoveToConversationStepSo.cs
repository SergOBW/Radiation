using UnityEngine;
using Cysharp.Threading.Tasks;

public enum MoveTargetMode
{
    WorldPosition,
    WaypointId,
    ActorId
}

[CreateAssetMenu(menuName = "Conversation/Step/MoveTo")]
public sealed class MoveToConversationStepSo : ConversationStepSo
{
    public MoveTargetMode targetMode = MoveTargetMode.WaypointId;

    [Header("World Position")]
    public Vector3 worldPosition;

    [Header("Waypoint")]
    public string waypointId;

    [Header("Actor")]
    public string targetActorId;

    [Header("Common")]
    public float stoppingDistance = 0.2f;
    public Vector3 offset;

    public override async UniTask Execute(ConversationContext context)
    {
        // базовые проверки + понятные логи
        var aid = (actorId ?? string.Empty).Trim();
        Debug.Log($"[MoveToStep] actorId='{aid}', mode={targetMode}");

        if (string.IsNullOrWhiteSpace(aid)) return;

        var bot = context.Registry.GetBot(aid);
        Debug.Log($"[MoveToStep] botFound={bot != null}");
        if (bot == null) return;

        if (!TryResolveTarget(context, out var targetPos))
        {
            Debug.LogWarning("[MoveToStep] Failed to resolve target position.");
            return;
        }

        var finalPos = targetPos + offset;
        Debug.Log($"[MoveToStep] GoTo: base={targetPos} + offset={offset} => final={finalPos}, stop={stoppingDistance:0.00}");

        await bot.MoveToAsync(finalPos, stoppingDistance, context.Token);
    }

    private bool TryResolveTarget(ConversationContext ctx, out Vector3 pos)
    {
        switch (targetMode)
        {
            case MoveTargetMode.WorldPosition:
                pos = worldPosition;
                Debug.Log($"[MoveToStep] Target: WorldPosition={pos}");
                return true;

            case MoveTargetMode.WaypointId:
            {
                var id = (waypointId ?? string.Empty).Trim();
                Debug.Log($"[MoveToStep] Target: WaypointId='{id}'");

                if (string.IsNullOrEmpty(id))
                {
                    pos = default;
                    return false;
                }

                // пробуем взять из контекста
                var registry = ctx.Waypoints;

                // фолбэк: найдём в сцене, если в контексте не передали
                if (registry == null)
                {
#if UNITY_2023_1_OR_NEWER
                    registry = Object.FindFirstObjectByType<WaypointRepository>();
#else
                    registry = Object.FindObjectOfType<WaypointRegistry>();
#endif
                    Debug.Log($"[MoveToStep] WaypointRegistry from scene: {(registry != null)}");
                }

                if (registry != null && registry.TryGet(id, out var tr) && tr != null)
                {
                    pos = tr.position;
                    Debug.Log($"[MoveToStep] Waypoint '{id}' -> {pos}");
                    return true;
                }

                Debug.LogWarning($"[MoveToStep] Waypoint '{id}' not found.");
                pos = default;
                return false;
            }

            case MoveTargetMode.ActorId:
            {
                var tid = (targetActorId ?? string.Empty).Trim();
                Debug.Log($"[MoveToStep] Target: ActorId='{tid}'");

                if (!string.IsNullOrEmpty(tid))
                {
                    var t = ctx.Registry.GetRoot(tid);
                    if (t != null)
                    {
                        pos = t.position;
                        Debug.Log($"[MoveToStep] Actor '{tid}' -> {pos}");
                        return true;
                    }
                    Debug.LogWarning($"[MoveToStep] Actor '{tid}' not found.");
                }

                pos = default;
                return false;
            }
        }

        pos = default;
        return false;
    }
}
