using UnityEngine;

/// <summary>
/// Управление анимациями игрока
/// Ищет Animator на модели (дочернем объекте)
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private CharacterController characterController;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Ищем Animator на себе или на дочерних объектах (модели)
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator == null)
        {
            Debug.LogWarning("Animator не найден на игроке или модели!");
        }
        else
        {
            Debug.Log($"✓ Animator найден: {animator.gameObject.name}");
        }
    }

    void Update()
    {
        if (animator == null || characterController == null) return;

        // Пропускаем, если нет контроллера
        if (animator.runtimeAnimatorController == null) return;

        // Получаем скорость из PlayerMovement (точнее чем CharacterController.velocity)
        PlayerMovement playerMovement = GetComponent<PlayerMovement>();
        float speed = 0f;
        
        if (playerMovement != null)
        {
            if (playerMovement.HasTarget())
            {
                speed = playerMovement.GetMoveSpeed();
            }
            else
            {
                speed = 0f;
            }
        }

        // Устанавливаем параметры анимации
        animator.SetFloat("Speed", speed);
        animator.SetBool("IsMoving", speed > 0.1f);
    }
}
