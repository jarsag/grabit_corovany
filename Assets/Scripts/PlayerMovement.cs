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
    public float stopDistance = 0.1f;

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
    private bool isFollowingMouse = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        mainCamera = Camera.main;
        targetPosition = transform.position;
        
        if (groundLayer == -1)
        {
            groundLayer = LayerMask.GetMask("Default");
        }
        
        Debug.Log($"PlayerMovement Start: camera={mainCamera != null}, controller={characterController != null}, layer={groundLayer}");
        
        // Проверка Input System
        if (Mouse.current != null)
        {
            Debug.Log("Input System: Mouse доступен");
        }
        else
        {
            Debug.LogWarning("Input System: Mouse НЕ доступен! Проверь Input System package.");
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
            isFollowingMouse = true;
            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            
            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, groundLayer))
            {
                SetDestination(hit.point);
            }
        }
        else
        {
            isFollowingMouse = false;
        }

        // При отпускании - ставим маркер и идём в точку
        if (leftReleased)
        {
            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            
            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, groundLayer))
            {
                SetDestinationWithMarker(hit.point);
            }
        }
    }

    private bool wasLeftPressedLastFrame = false;

    void SetDestination(Vector3 position)
    {
        targetPosition = position;
        targetPosition.y = transform.position.y;
        hasTarget = true;
    }

    void SetDestinationWithMarker(Vector3 position)
    {
        targetPosition = position;
        targetPosition.y = transform.position.y;
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
        direction.y = 0;

        float distanceToTarget = direction.magnitude;

        if (distanceToTarget < stopDistance)
        {
            hasTarget = false;
            RemoveMarker();
            return;
        }

        direction.Normalize();
        Vector3 move = direction * moveSpeed * Time.deltaTime;
        characterController.Move(move);

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }
        
        Debug.Log($"Движение: dir={direction}, speed={moveSpeed}, pos={transform.position}");
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
}
