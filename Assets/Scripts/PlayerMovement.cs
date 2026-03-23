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
    private bool wasLeftPressedLastFrame = false;

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

        Debug.Log($"ЛКМ: pressed={leftPressed}, released={leftReleased}");

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
