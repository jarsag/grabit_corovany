using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Изометрическая камера, следующая за игроком с возможностью вращения
/// </summary>
public class IsometricCamera : MonoBehaviour
{
    [Header("Цель")]
    [Tooltip("Игрок, за которым следует камера")]
    public Transform target;

    [Header("Позиция камеры")]
    [Tooltip("Дистанция камеры от игрока")]
    public float distance = 12f;
    
    [Tooltip("Высота камеры (угол обзора)")]
    [Range(0f, 90f)]
    public float heightAngle = 35f;
    
    [Tooltip("Минимальная дистанция зума")]
    public float minDistance = 5f;
    
    [Tooltip("Максимальная дистанция зума")]
    public float maxDistance = 20f;

    [Header("Вращение")]
    [Tooltip("Скорость вращения камеры")]
    public float rotateSpeed = 5f;
    
    [Tooltip("Минимальный угол высоты")]
    [Range(0f, 89f)]
    public float minVerticalAngle = 5f;
    
    [Tooltip("Максимальный угол высоты")]
    [Range(1f, 90f)]
    public float maxVerticalAngle = 85f;

    [Header("Настройки слежения")]
    [Tooltip("Скорость плавного следования за целью")]
    [Range(0.1f, 20f)]
    public float followSpeed = 5f;
    
    [Tooltip("Использовать плавное следование")]
    public bool smoothFollow = true;

    private float horizontalAngle = 0f;
    private float verticalAngle = 35f;
    private Vector3 currentPosition;

    void Start()
    {
        if (target != null)
        {
            // Инициализируем углы из текущей позиции
            Vector3 offset = transform.position - target.position;
            horizontalAngle = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
            verticalAngle = Mathf.Asin(offset.y / offset.magnitude) * Mathf.Rad2Deg;
        }
        else
        {
            // Углы по умолчанию
            verticalAngle = heightAngle;
        }
        currentPosition = transform.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Вращение камеры при зажатой ПКМ
        if (Mouse.current != null && Mouse.current.rightButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            
            horizontalAngle += delta.x * rotateSpeed * 0.1f;
            verticalAngle -= delta.y * rotateSpeed * 0.1f;
            
            // Ограничиваем вертикальный угол
            verticalAngle = Mathf.Clamp(verticalAngle, minVerticalAngle, maxVerticalAngle);
        }

        // Зум колёсиком мыши
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll != 0)
            {
                distance -= scroll * 0.5f;
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
            }
        }

        // Вычисляем позицию камеры из углов
        float vRad = verticalAngle * Mathf.Deg2Rad;
        float hRad = horizontalAngle * Mathf.Deg2Rad;
        
        Vector3 targetPosition = target.position;
        targetPosition.y += Mathf.Sin(vRad) * distance;
        targetPosition.x += Mathf.Cos(vRad) * Mathf.Sin(hRad) * distance;
        targetPosition.z += Mathf.Cos(vRad) * Mathf.Cos(hRad) * distance;

        currentPosition = targetPosition;

        if (smoothFollow)
        {
            transform.position = Vector3.Lerp(transform.position, currentPosition, followSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = currentPosition;
        }

        transform.LookAt(target.position);
    }

    void OnDrawGizmos()
    {
        if (target == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, target.position);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(target.position, 0.3f);
    }
}
