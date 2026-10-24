using LumberJack.Core;
using LumberJack.Inventory;
using LumberJack.Shared;
using UnityEngine;

namespace LumberJack.Tools
{
    /// <summary>
    /// Ferramenta solta no mundo: mostra a dica ao olhar e vai para o inventário ao interagir.
    /// </summary>
    [RequireComponent(typeof(ItemReference))]
    public class Tool : MonoBehaviour, IDisplayable, IInteractable
    {
        private ItemReference _itemReference;
        // Montada uma vez: a tecla não muda durante o jogo e assim a dica não aloca string
        private string _pickUpHint;

        private void Awake()
        {
            _itemReference = GetComponent<ItemReference>();
        }

        private void Start()
        {
            _pickUpHint = $"Press {InputManager.Instance.InteractKeyLabel} to pick up the tool";
        }

        public void Display()
        {
            UIDisplays.Instance.ShowKeyInfo(_pickUpHint);
        }

        public void Interact()
        {
            // Só some do mundo se coube no inventário
            if (InventoryManager.Instance.TryAddItem(_itemReference.Item)) Destroy(gameObject);
        }
    }
}
