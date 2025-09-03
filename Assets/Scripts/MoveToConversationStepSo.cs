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
        Debug.Log($"IsNullOrWhiteSpace= {actorId}" );
        if (string.IsNullOrWhiteSpace(actorId)) return;

        var bot = context.Registry.GetBot(actorId);
        Debug.Log($"is bot = {bot}" );
        if (bot == null) return;

        Vector3 targetPos;
        bool ok = TryResolveTarget(context, out targetPos);
        Debug.Log($"is ok = {ok}" );
        if (!ok) return;

        targetPos += offset;
        Debug.Log("Executing MoveTo");
        await bot.MoveToAsync(targetPos, stoppingDistance, context.Token);
    }

    private bool TryResolveTarget(ConversationContext ctx, out Vector3 pos)
    {
        switch (targetMode)
        {
            case MoveTargetMode.WorldPosition:
                pos = worldPosition;
                return true;

            case MoveTargetMode.WaypointId:
                if (!string.IsNullOrWhiteSpace(waypointId) &&
                    ctx.Waypoints.TryGet(waypointId, out var transform) && transform != null)
                {
                    pos = transform.position;
                    return true;
                }
                break;

            case MoveTargetMode.ActorId:
                if (!string.IsNullOrWhiteSpace(targetActorId))
                {
                    var t = ctx.Registry.GetRoot(targetActorId);
                    if (t != null)
                    {
                        pos = t.position;
                        return true;
                    }
                }
                break;
        }

        pos = default;
        return false;
    }
}
