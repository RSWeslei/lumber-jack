using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Hotbar : MonoBehaviour
{
    [SerializeField] private Hand hand;
    public VisualElement m_hotbar;
    public List<Slot> slots = new List<Slot>();
    public Item testItem;
    private int previousSlot = 0;

    private void Awake() {
       CreateSlots();
       SelectSlot(0);
    }

    private void CreateSlots() {
        var root = GetComponent<UIDocument>().rootVisualElement; // Pega o root do UI Document
        m_hotbar = root.Q<VisualElement>("hotbar"); // Pega o elemento visual com o nome "hotbar"

        for (int i = 0; i < m_hotbar.childCount; i++) {
            slots.Add(m_hotbar[i] as Slot);
        }

        // Testing
        slots[0].AddItem(testItem);
        SelectSlot(1);
    }

    public void SelectSlot(int slotNumber) {
        if (slotNumber == previousSlot) return;
        
        slots[slotNumber].AddToClassList("selected");
        slots[previousSlot].RemoveFromClassList("selected");
        hand.LoadItem(slots[slotNumber].Item); // Carrega o item no slot selecionado
        previousSlot = slotNumber;
    }

    public void SelectSlotScroll(float scroll) {
        if (scroll > 0) {
            if (previousSlot == slots.Count-1) {
                SelectSlot(0);
            } else {
                SelectSlot(previousSlot+1);
            }
        } else if (scroll < 0) {
            if (previousSlot == 0) {
                SelectSlot(slots.Count-1);
            } else {
                SelectSlot(previousSlot-1);
            }
        }
    }
}
