using UnityEngine;

/// <summary>
/// Маркер точки назначения - исчезает через несколько секунд
/// </summary>
public class DestinationMarker : MonoBehaviour
{
    [Tooltip("Время жизни маркера в секундах")]
    public float lifetime = 2f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
