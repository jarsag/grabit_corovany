using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Управление анимациями игрока
/// Ищет Animator на модели (дочернем объекте)
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerAnimation : MonoBehaviour
{
    [Header("Отладка анимации (клавиши работают в Play)")]
    [Tooltip("Включить: [ и ] — медленнее/быстрее, P — пауза, . и , — шаг на кадр, R — сброс")]
    public bool debugHotkeys = true;

    private float debugScale = 1f;
    private bool debugPaused;

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

        // --- Отладка: замедление и покадровый просмотр ---
        if (debugHotkeys)
        {
            HandleDebugKeys();
            animator.speed = debugPaused ? 0f : debugScale;
        }
        else
        {
            animator.speed = 1f;
        }
    }

    /// <summary>
    /// [ и ] — медленнее/быстрее, P — пауза, . и , — шаг на один кадр (1/30 с), R — сброс.
    /// </summary>
    void HandleDebugKeys()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.leftBracketKey.wasPressedThisFrame)
        {
            debugScale = Mathf.Max(0.01f, debugScale * 0.5f);
            debugPaused = false;
            Debug.Log($"[Анимация] скорость x{debugScale:0.###} (P — пауза, . и , — шаг)");
        }
        if (kb.rightBracketKey.wasPressedThisFrame)
        {
            debugScale = Mathf.Min(8f, debugScale * 2f);
            debugPaused = false;
            Debug.Log($"[Анимация] скорость x{debugScale:0.###}");
        }
        if (kb.rKey.wasPressedThisFrame)
        {
            debugScale = 1f;
            debugPaused = false;
            Debug.Log("[Анимация] скорость x1, пауза снята");
        }
        if (kb.pKey.wasPressedThisFrame)
        {
            debugPaused = !debugPaused;
            Debug.Log(debugPaused
                ? "[Анимация] ПАУЗА. Клавиши . и , — шаг на один кадр"
                : "[Анимация] продолжаю");
        }

        if (debugPaused)
        {
            if (kb.periodKey.wasPressedThisFrame) StepOneFrame(1f);
            if (kb.commaKey.wasPressedThisFrame) StepOneFrame(-1f);
        }
    }

    void StepOneFrame(float direction)
    {
        AnimatorStateInfo st = animator.GetCurrentAnimatorStateInfo(0);
        float len = st.length > 0.001f ? st.length : (1f / 30f);
        float nt = st.normalizedTime + direction * (1f / 30f) / len;
        nt -= Mathf.Floor(nt);              // зацикливаем в 0..1
        animator.Play(st.fullPathHash, 0, nt);
        animator.Update(0f);
        Debug.Log($"[Анимация] кадр {(nt * len * 30f):0} из {(len * 30f):0} (нормализованное время {nt:0.###})");
    }
}
