using System.Collections.Generic;
using LumberJack.Core;
using LumberJack.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumberJack.Inventory
{
    /// <summary>
    /// Hotbar do jogador: guarda itens nos slots do UI Toolkit e equipa na mão o item do slot
    /// selecionado. Selecionar de novo o slot atual guarda o item (mão vazia). Ao trocar, mostra
    /// o nome do item por um instante acima da barra.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class Hotbar : MonoBehaviour
    {
        private const string SlotContainerName = "hotbar";
        private const string SelectedClass = "hotbar__slot--selected";
        private const string ItemNameLabelName = "itemName";
        private const string ItemNameVisibleClass = "hotbar__item-name--visible";

        [SerializeField] private Hand _hand;
        [Tooltip("Quanto tempo (ms) o nome do item fica visível depois de trocar de slot")]
        [SerializeField] private long _itemNameDurationMs = 1500;

        private readonly List<Slot> _slots = new();
        private Label _itemName;
        private IVisualElementScheduledItem _hideItemNameTask;
        private int _selectedIndex;
        private bool _isSlotEquipped;

        private void Awake()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            root.Q<VisualElement>(SlotContainerName).Query<Slot>().ToList(_slots);
            _itemName = root.Q<Label>(ItemNameLabelName);
            // Criado uma vez e só reagendado depois (sem alocar a cada troca)
            _hideItemNameTask = _itemName.schedule.Execute(HideItemName);
            _hideItemNameTask.Pause();

            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].SetKeyLabel(InputManager.Instance.GetHotbarKeyLabel(i));
            }
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
                // Caiu no slot que já está selecionado: equipa na hora
                if (_isSlotEquipped && i == _selectedIndex)
                {
                    _hand.LoadItem(item);
                    ShowItemName(item);
                }
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
            ShowItemName(_slots[index].Item);
        }

        private void UnequipSlot()
        {
            _slots[_selectedIndex].EnableInClassList(SelectedClass, false);
            _isSlotEquipped = false;
            _hand.UnloadItemAndDestroy();
            HideItemName();
        }

        private void ShowItemName(ItemDefinition item)
        {
            if (item == null)
            {
                HideItemName();
                return;
            }

            _itemName.text = item.DisplayName;
            _itemName.EnableInClassList(ItemNameVisibleClass, true);
            _hideItemNameTask.ExecuteLater(_itemNameDurationMs);
        }

        private void HideItemName()
        {
            _itemName.EnableInClassList(ItemNameVisibleClass, false);
        }
    }
}
