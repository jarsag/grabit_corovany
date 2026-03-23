using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Игрок с перемещением по клику правой кнопки мыши
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Настройки движения")]
    [Tooltip("Скорость перемещения игрока")]
    public float moveSpeed = 5f;

    [Tooltip("Минимальная дистанция до цели, чтобы считать её достигнутой")]
    public float stopDistance = 0.3f;

    [Header("Настройки поверхности")]
    [Tooltip("Слой земли для raycast")]
    public LayerMask groundLayer = -1;

    [Tooltip("Максимальная дистанция raycast")]
    public float raycastDistance = 100f;

    [Header("Визуализация")]
    [Tooltip("Префаб маркера точки назначения")]
    public GameObject destinationMarker;

    [Tooltip("Смещение маркера над поверхностью")]
    public float markerOffset = 0.1f;

    private CharacterController characterController;
    private Camera mainCamera;
    private Vector3 targetPosition;
    private bool hasTarget = false;
    private GameObject currentMarker;
    private bool wasLeftPressedLastFrame = false;
    private float lastMoveSpeed = 0f;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        mainCamera = Camera.main;
        targetPosition = transform.position;

        if (groundLayer == -1)
        {
            groundLayer = LayerMask.GetMask("Default");
        }
    }

    void Update()
    {
        HandleInput();
        MoveToTarget();
    }

    void HandleInput()
    {
        if (mainCamera == null)
        {
            Debug.LogWarning("Camera.main = null!");
            return;
        }

        Vector2 mousePos = Vector2.zero;
        bool leftPressed = false;
        bool leftReleased = false;

        if (Mouse.current != null)
        {
            mousePos = Mouse.current.position.ReadValue();
            leftPressed = Mouse.current.leftButton.isPressed;

            // Определяем отпускание кнопки
            if (!leftPressed && wasLeftPressedLastFrame)
            {
                leftReleased = true;
            }
        }

        wasLeftPressedLastFrame = leftPressed;

        // Если ЛКМ зажата - следуем за курсором (без маркеров)
        if (leftPressed)
        {
            Ray ray = mainCamera.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, groundLayer))
            {
                Debug.Log($"Raycast попал в {hit.point}");
                SetDestination(hit.point);
            }
            else
            {
                Debug.LogWarning($"Raycast не попал! Layer={groundLayer}, distance={raycastDistance}");
            }
        }

        // При отпускании - ставим маркер и идём в точку
        if (leftReleased)
        {
            Ray ray = mainCamera.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, groundLayer))
            {
                Debug.Log($"Raycast попал (отпускание) в {hit.point}");
                SetDestinationWithMarker(hit.point);
            }
        }
    }

    void SetDestination(Vector3 position)
    {
        // Используем позицию от raycast (с высотой земли)
        targetPosition = position;
        hasTarget = true;
    }

    void SetDestinationWithMarker(Vector3 position)
    {
        // Используем позицию от raycast (с высотой земли)
        targetPosition = position;
        hasTarget = true;

        if (destinationMarker != null)
        {
            RemoveMarker();
            currentMarker = Instantiate(destinationMarker, position + Vector3.up * markerOffset, Quaternion.identity);
        }
    }

    void MoveToTarget()
    {
        if (!hasTarget) return;

        Vector3 direction = targetPosition - transform.position;

        // Движение по горизонтали
        Vector3 horizontalDirection = direction;
        horizontalDirection.y = 0;

        float distanceToTarget = horizontalDirection.magnitude;

        if (distanceToTarget < stopDistance)
        {
            hasTarget = false;
            RemoveMarker();
            
            // Явно сбрасываем "скорость" для анимации
            lastMoveSpeed = 0f;
            
            return;
        }

        horizontalDirection.Normalize();

        // Плавное замедление при приближении к цели
        float speedMultiplier = Mathf.Clamp01(distanceToTarget / 0.5f);
        Vector3 move = horizontalDirection * moveSpeed * speedMultiplier * Time.deltaTime;
        
        // Сохраняем текущую скорость для анимации
        lastMoveSpeed = move.magnitude / Time.deltaTime;

        // Добавляем вертикальное движение для приземления на землю
        float verticalMove = 0f;

        // Raycast вниз для проверки высоты земли
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit groundHit, 2f, groundLayer))
        {
            // Игрок над землёй - опускаем
            float groundDistance = groundHit.distance;
            if (groundDistance > 0.1f)
            {
                verticalMove = -groundDistance * 5f * Time.deltaTime;
            }
        }

        move.y = verticalMove;
        characterController.Move(move);

        if (horizontalDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(horizontalDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }
    }

    void RemoveMarker()
    {
        if (currentMarker != null)
        {
            Destroy(currentMarker);
            currentMarker = null;
        }
    }

    void OnDestroy()
    {
        RemoveMarker();
    }

    /// <summary>
    /// Проверяет, есть ли у игрока активная цель движения
    /// </summary>
    public bool HasTarget()
    {
        return hasTarget;
    }

    /// <summary>
    /// Возвращает текущую скорость движения (для анимации)
    /// </summary>
    public float GetMoveSpeed()
    {
        return lastMoveSpeed;
    }
}
