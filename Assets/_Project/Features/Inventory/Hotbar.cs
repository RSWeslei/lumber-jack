using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Managers;

public class Hotbar : MonoBehaviour
{
    public VisualElement m_hotbar;
    public List<Slot> slots = new List<Slot>();
    public Item testItem;
    [SerializeField] private Hand _hand;
    private int _previousSlot = 0;
    private bool _sameSlot = false;

    private void Awake()
    {
        CreateSlots();
        SelectSlot(0);
    }

    void OnEnable()
    {
        InputManager.Instance.OnHotbarNumberInput += SelectSlot;
        InputManager.Instance.OnHotbarScrollInput += SelectSlotScroll;
    }

    void OnDisable()
    {
        InputManager.Instance.OnHotbarNumberInput -= SelectSlot;
        InputManager.Instance.OnHotbarScrollInput -= SelectSlotScroll;
    }

    private void CreateSlots()
    {
        var root = GetComponent<UIDocument>().rootVisualElement; // Pega o root do UI Document
        m_hotbar = root.Q<VisualElement>("hotbar"); // Pega o elemento visual com o nome "hotbar"

        for (int i = 0; i < m_hotbar.childCount; i++)
        {
            slots.Add(m_hotbar[i] as Slot);
        }

        // Testing
        // slots[0].AddItem(testItem);
        // SelectSlot(1);
    }

    public bool AddItem(Item item)
    {
        foreach (var slot in slots)
        {
            if (slot.Item == null)
            {
                slot.AddItem(item);
                return true;
            }
        }
        return false;
    }

    public void SelectSlot(int slotNumber)
    {
        if (slotNumber == _previousSlot && !_sameSlot)
        {
            _hand.UnloadItemAndDestroy();
            _sameSlot = true;
            slots[_previousSlot].RemoveFromClassList("selected");
            return;
        }

        slots[_previousSlot].RemoveFromClassList("selected");
        slots[slotNumber].AddToClassList("selected");
        _hand.LoadItem(slots[slotNumber].Item); // Carrega o item no slot selecionado
        _previousSlot = slotNumber;
        _sameSlot = false;
    }

    public void SelectSlotScroll(float scroll)
    {
        if (scroll > 0)
        {
            if (_previousSlot == slots.Count - 1)
            {
                SelectSlot(0);
            }
            else
            {
                SelectSlot(_previousSlot + 1);
            }
        }
        else if (scroll < 0)
        {
            if (_previousSlot == 0)
            {
                SelectSlot(slots.Count - 1);
            }
            else
            {
                SelectSlot(_previousSlot - 1);
            }
        }
    }
}
