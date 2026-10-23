using UnityEngine;
using UnityEngine.InputSystem;

// Coloca um tronco na cena e, apertando R, limpa os pedacos e coloca um tronco novo.
public class LogSpawner : MonoBehaviour
{
    [SerializeField] private CuttableWood logPrefab;

    private void Start()
    {
        Spawn();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            // Os pedacos cortados nascem como filhos do mesmo pai do tronco (este objeto)
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
            Spawn();
        }
    }

    private void Spawn()
    {
        if (logPrefab == null)
        {
            Debug.LogError("LogSpawner sem prefab do tronco. Arraste Features/Wood/Prefabs/Log.prefab no campo Log Prefab.", this);
            return;
        }
        Instantiate(logPrefab, transform.position, transform.rotation * logPrefab.transform.rotation, transform);
    }
}
