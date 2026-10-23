using UnityEngine;
using UnityEngine.InputSystem;

namespace LumberJack.Wood
{
    /// <summary>
    /// So para a cena de teste: coloca um tronco e, apertando R, limpa os pedacos e coloca outro.
    /// </summary>
    public class LogSpawner : MonoBehaviour
    {
        [SerializeField] private CuttableWood _logPrefab;

        private void Start()
        {
            Spawn();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.rKey.wasPressedThisFrame) return;

            // Os pedacos cortados nascem como filhos do mesmo pai do tronco (este objeto)
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
            Spawn();
        }

        private void Spawn()
        {
            if (_logPrefab == null)
            {
                Debug.LogError($"{nameof(LogSpawner)} sem prefab do tronco. Arraste Features/Wood/Prefabs/Log.prefab no campo Log Prefab.", this);
                return;
            }
            Instantiate(_logPrefab, transform.position, transform.rotation * _logPrefab.transform.rotation, transform);
        }
    }
}
