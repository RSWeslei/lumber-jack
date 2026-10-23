using System.Collections.Generic;
using LumberJack.Core;
using LumberJack.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumberJack.Inventory
{
    /// <summary>
    /// Hotbar do jogador: guarda itens nos slots do UI Toolkit e equipa na mao o item do slot
    /// selecionado. Selecionar de novo o slot atual guarda o item (mao vazia).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class Hotbar : MonoBehaviour
    {
        private const string SlotContainerName = "hotbar";
        private const string SelectedClass = "hotbar__slot--selected";

        [SerializeField] private Hand _hand;

        private readonly List<Slot> _slots = new List<Slot>();
        private int _selectedIndex;
        private bool _isSlotEquipped;

        private void Awake()
        {
            VisualElement container = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>(SlotContainerName);
            container.Query<Slot>().ToList(_slots);
            SelectSlot(0);
        }

        private void OnEnable()
        {
            InputManager.Instance.HotbarSlotSelected += HandleHotbarSlotSelected;
            InputManager.Instance.HotbarScrolled += HandleHotbarScrolled;
        }

        private void OnDisable()
        {
            InputManager.Instance.HotbarSlotSelected -= HandleHotbarSlotSelected;
            InputManager.Instance.HotbarScrolled -= HandleHotbarScrolled;
        }

        /// <summary>
        /// Guarda o item no primeiro slot vazio. Retorna false se a hotbar estiver cheia.
        /// </summary>
        public bool TryAddItem(ItemDefinition item)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (!_slots[i].IsEmpty) continue;

                _slots[i].SetItem(item);
                // Caiu no slot que ja esta selecionado: equipa na hora
                if (_isSlotEquipped && i == _selectedIndex) _hand.LoadItem(item);
                return true;
            }
            return false;
        }

        private void HandleHotbarSlotSelected(int index)
        {
            if (index < 0 || index >= _slots.Count) return;

            if (index == _selectedIndex && _isSlotEquipped) UnequipSlot();
            else SelectSlot(index);
        }

        private void HandleHotbarScrolled(float direction)
        {
            int step = direction > 0f ? 1 : -1;
            SelectSlot((_selectedIndex + step + _slots.Count) % _slots.Count);
        }

        private void SelectSlot(int index)
        {
            _slots[_selectedIndex].EnableInClassList(SelectedClass, false);
            _slots[index].EnableInClassList(SelectedClass, true);
            _selectedIndex = index;
            _isSlotEquipped = true;
            _hand.LoadItem(_slots[index].Item);
        }

        private void UnequipSlot()
        {
            _slots[_selectedIndex].EnableInClassList(SelectedClass, false);
            _isSlotEquipped = false;
            _hand.UnloadItemAndDestroy();
        }
    }
}
