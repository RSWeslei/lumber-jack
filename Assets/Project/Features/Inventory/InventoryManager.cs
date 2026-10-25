using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LumberJack.Inventory
{
    /// <summary>
    /// Ponto de entrada para guardar itens no inventário do jogador (hoje só a hotbar).
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        [SerializeField] private Hotbar _hotbar;
        [Tooltip("Itens que o jogador já tem ao começar, na ordem dos slots")]
        [SerializeField] private List<ItemDefinition> _startingItems = new List<ItemDefinition>();

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

        // Start: a hotbar já montou os slots no Awake dela
        private void Start()
        {
            foreach (ItemDefinition t in _startingItems.Where(t => t != null))
            {
                TryAddItem(t);
            }
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
