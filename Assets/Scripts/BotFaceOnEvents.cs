using UnityEngine;
using UnityEngine.AI;
using VContainer;

[RequireComponent(typeof(BotController))]
public sealed class BotFaceLean : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private NavMeshAgent agent;       // Родитель-агент (истина по позиции/курсу)
    [SerializeField] private Transform rotateTarget;   // Кого крутим визуально (обычно botRoot)
    [SerializeField] private Transform player;         // Цель взгляда (если пусто — возьмём по тегу)

    [Inject] private ConversationOrchestrator _orchestrator; // опционально; если null — будем вращать всегда, когда стоим

    [Header("Params")]
    [SerializeField] private bool onlyWhenTalking = true;     // Вращать к игроку только если идёт разговор
    [SerializeField, Range(0.01f,1f)] private float smoothTime = 0.2f;
    [SerializeField, Range(60f,1080f)] private float maxDegPerSec = 540f;
    [SerializeField, Range(0f,20f)] private float deadZone = 5f;

    private BotController _bot;
    private float _yawVel;
    private bool _wasMoving; // для обнаружения фронта "start moving"

    private void Awake()
    {
        _bot = GetComponent<BotController>();
        if (!agent) agent = GetComponentInParent<NavMeshAgent>();
        if (!rotateTarget) rotateTarget = agent ? agent.transform : transform;
        if (!player)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) player = go.transform;
        }
    }

    private void Update()
    {
        if (!_bot || !agent || !rotateTarget || !player) return;

        bool isMoving = _bot.IsMoving;

        // фронт "начали идти" — сбрасываем угол, отдаём поворот агенту
        if (isMoving && !_wasMoving)
        {
            agent.updateRotation = true;                       // агент рулит в пути
            rotateTarget.rotation = agent.transform.rotation;  // СБРОС УГЛА: выравниваем визуал под курс агента
        }
        _wasMoving = isMoving;

        if (isMoving)
            return; // в движении — вообще не трогаем поворот

        // стоим: забираем поворот на себя
        agent.updateRotation = false;

        // политика разговора
        if (onlyWhenTalking && (_orchestrator == null || !_orchestrator.IsRunning))
            return;

        // мягкий доворот к игроку по Y
        Vector3 to = player.position - rotateTarget.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.0001f) return;

        float targetYaw  = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
        float currentYaw = rotateTarget.eulerAngles.y;
        float delta      = Mathf.DeltaAngle(currentYaw, targetYaw);

        if (Mathf.Abs(delta) < deadZone) return;

        float newYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref _yawVel, smoothTime, maxDegPerSec, Time.deltaTime);
        rotateTarget.rotation = Quaternion.Euler(0f, newYaw, 0f);
    }
}
