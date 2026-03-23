using UnityEngine;

/// <summary>
/// Изометрическая камера, следующая за игроком
/// </summary>
public class IsometricCamera : MonoBehaviour
{
    [Header("Цель")]
    [Tooltip("Игрок, за которым следует камера")]
    public Transform target;

    [Header("Позиция камеры")]
    [Tooltip("Смещение камеры относительно игрока (изометрический угол ~35°)")]
    public Vector3 offset = new Vector3(0, 10, -8);

    [Header("Настройки слежения")]
    [Tooltip("Скорость плавного следования за целью")]
    [Range(0.1f, 20f)]
    public float followSpeed = 5f;
    
    [Tooltip("Использовать плавное следование")]
    public bool smoothFollow = true;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPosition = target.position + offset;

        if (smoothFollow)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = targetPosition;
        }

        transform.LookAt(target.position);
    }

    void OnDrawGizmos()
    {
        if (target == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, target.position);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(target.position + offset, 0.5f);
    }
}
