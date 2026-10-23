using UnityEngine;

namespace LumberJack.Inventory
{
    /// <summary>
    /// Liga um objeto do mundo ao item que ele representa (ex.: o machado no chao).
    /// </summary>
    public class ItemReference : MonoBehaviour
    {
        [SerializeField] private ItemDefinition _item;

        public ItemDefinition Item => _item;
    }
}
