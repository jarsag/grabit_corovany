using UnityEngine;

/// <summary>
/// Управление анимациями игрока
/// Автоматически находит Animator и управляет параметрами
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private CharacterController characterController;
    
    // Хэш параметров для производительности
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    void Start()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        
        if (animator == null)
        {
            Debug.LogWarning("Animator не найден на игроке!");
        }
    }

    void Update()
    {
        if (animator == null || characterController == null) return;

        // Вычисляем скорость движения
        float speed = characterController.velocity.magnitude;
        
        // Устанавливаем параметры анимации
        animator.SetFloat(SpeedHash, speed);
        animator.SetBool(IsMovingHash, speed > 0.1f);
    }
}
