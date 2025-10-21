using UnityEngine;

/// Держит пояс на фиксированной высоте и центрирует его под игроком.
/// Работает в мировых координатах; позиция/поворот обновляются в LateUpdate.
public sealed class BeltFollower : MonoBehaviour
{
    [Header("References")]
    [Tooltip("XR Origin или любой объект, позиция Y которого считается полом (обычно XR Origin).")]
    [SerializeField] private Transform xrOrigin;
    [Tooltip("Главная камера (HMD) внутри XR Origin, обычно MainCamera под Camera Offset).")]
    [SerializeField] private Transform head;

    [Header("Placement (meters)")]
    [Tooltip("Высота пояса над полом (от Y XR Origin).")]
    [SerializeField] private float heightFromFloor = 0.90f;
    [Tooltip("Смещение вперёд относительно направления головы.")]
    [SerializeField] private float forwardOffset = 0.10f;
    [Tooltip("Смещение вправо (+) / влево (-) относительно направления головы.")]
    [SerializeField] private float lateralOffset = 0.00f;

    [Header("Orientation")]
    [Tooltip("Выравнивать пояс по yaw головы (только по горизонту).")]
    [SerializeField] private bool alignToHeadYaw = true;
    [Tooltip("Доп. сдвиг пояса по yaw в градусах (например, 180 чтобы смотреть «от игрока»).")]
    [SerializeField] private float extraYawOffsetDeg = 0f;

    [Header("Smoothing")]
    [Tooltip("Скорость сглаживания позиции (units/sec). 0 = без сглаживания.")]
    [SerializeField] private float positionLerpSpeed = 15f;
    [Tooltip("Скорость сглаживания поворота (deg/sec). 0 = без сглаживания.")]
    [SerializeField] private float rotationLerpSpeed = 360f;

    void LateUpdate()
    {
        if (head == null || xrOrigin == null)
            return;

        // Базовая точка под головой на уровне пола XR Origin
        float floorY = xrOrigin.position.y;
        Vector3 baseOnFloor = new Vector3(head.position.x, floorY + heightFromFloor, head.position.z);

        // Горизонтальный yaw головы
        float headYaw = head.eulerAngles.y + extraYawOffsetDeg;
        Quaternion yawRot = Quaternion.Euler(0f, headYaw, 0f);

        // Смещения вперёд/вбок относительно yaw
        Vector3 fwd = yawRot * Vector3.forward;
        Vector3 right = yawRot * Vector3.right;

        Vector3 targetPos = baseOnFloor + fwd * forwardOffset + right * lateralOffset;
        Quaternion targetRot = alignToHeadYaw ? yawRot : transform.rotation;

        // Применяем сглаживание (или телепорт, если скорость = 0)
        if (positionLerpSpeed > 0f)
            transform.position = Vector3.Lerp(transform.position, targetPos, 1f - Mathf.Exp(-positionLerpSpeed * Time.deltaTime));
        else
            transform.position = targetPos;

        if (alignToHeadYaw)
        {
            if (rotationLerpSpeed > 0f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationLerpSpeed * Time.deltaTime);
            else
                transform.rotation = targetRot;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (head == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(new Vector3(head.position.x, (xrOrigin ? xrOrigin.position.y : 0f) + heightFromFloor, head.position.z), 0.02f);
    }
#endif
}
