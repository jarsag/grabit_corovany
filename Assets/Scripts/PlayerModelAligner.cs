using UnityEngine;

/// <summary>
/// Выравнивает модель игрока относительно CharacterController
/// </summary>
[ExecuteInEditMode]
public class PlayerModelAligner : MonoBehaviour
{
    [Tooltip("Автоматически выравнивать при старте")]
    public bool alignOnStart = true;
    
    [Tooltip("Смещение модели по Y")]
    public float yOffset = 0f;

    void Start()
    {
        if (alignOnStart)
        {
            AlignModel();
        }
    }

    [ContextMenu("Выровнять модель")]
    public void AlignModel()
    {
        CharacterController controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogWarning("CharacterController не найден!");
            return;
        }

        // Ищем дочерний объект с моделью
        Transform modelTransform = null;
        foreach (Transform child in transform)
        {
            if (child.name.Contains("Model") || child.name.Contains("4") || child.GetComponent<SkinnedMeshRenderer>() != null)
            {
                modelTransform = child;
                break;
            }
        }

        if (modelTransform == null)
        {
            Debug.LogWarning("Модель не найдена среди дочерних объектов!");
            return;
        }

        // Выравниваем модель
        modelTransform.localPosition = new Vector3(0, yOffset, 0);
        modelTransform.localRotation = Quaternion.identity;

        Debug.Log($"Модель выровнена: {modelTransform.name} (Y={yOffset})");
    }
}
