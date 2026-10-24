using UnityEngine;

namespace LumberJack.Inventory
{
    /// <summary>
    /// Ponto de entrada para guardar itens no inventário do jogador (hoje só a hotbar).
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        [SerializeField] private Hotbar _hotbar;

        public static InventoryManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Guarda o item no primeiro espaço livre. Retorna false se o inventário estiver cheio.
        /// </summary>
        public bool TryAddItem(ItemDefinition item)
        {
            return _hotbar.TryAddItem(item);
        }
    }
}
