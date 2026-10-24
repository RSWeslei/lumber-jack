using UnityEngine;
using UnityEngine.InputSystem;

namespace LumberJack.Wood
{
    /// <summary>
    /// Só para a cena de teste: coloca um tronco e, apertando R, limpa os pedaços e coloca outro.
    /// </summary>
    public class LogSpawner : MonoBehaviour
    {
        private const string ResetBinding = "<Keyboard>/r";

        [SerializeField] private CuttableWood _logPrefab;

        private bool _hasPrefab;
        // A cena de teste não tem InputManager: ação própria só para a tecla de recomeçar
        private InputAction _resetAction;

        private void Awake()
        {
            _hasPrefab = _logPrefab != null;
            if (!_hasPrefab)
            {
                Debug.LogError($"{nameof(LogSpawner)} sem prefab do tronco. Arraste Features/Wood/Prefabs/Log.prefab no campo Log Prefab.", this);
            }
            _resetAction = new InputAction("Reset", InputActionType.Button, ResetBinding);
        }

        private void OnEnable()
        {
            _resetAction.performed += HandleResetPerformed;
            _resetAction.Enable();
        }

        private void Start()
        {
            Spawn();
        }

        private void OnDisable()
        {
            _resetAction.performed -= HandleResetPerformed;
            _resetAction.Disable();
        }

        private void OnDestroy()
        {
            _resetAction.Dispose();
        }

        private void HandleResetPerformed(InputAction.CallbackContext context)
        {
            // Os pedaços cortados nascem como filhos do mesmo pai do tronco (este objeto).
            // De trás para frente: o índice dos que faltam não muda enquanto apaga
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
            Spawn();
        }

        private void Spawn()
        {
            if (!_hasPrefab) return;

            Instantiate(_logPrefab, transform.position, transform.rotation * _logPrefab.transform.rotation, transform);
        }
    }
}
