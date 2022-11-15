using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Hotbar : MonoBehaviour
{
    public VisualElement hotbar;
    public List<Slot> slots = new List<Slot>();
    public Item testItem;

    private void Awake() {
       CreateSlots();
    }

    private void CreateSlots() {
        var root = GetComponent<UIDocument>().rootVisualElement; // Pega o root do UI Document
        hotbar = root.Q<VisualElement>("hotbar"); // Pega o elemento visual com o nome "hotbar"

        for (int i = 0; i < hotbar.childCount; i++) {
            // Adiciona um slot em cada elemento visual filho do hotbar
            hotbar[i].Add(new Slot()); 
            Slot slot = hotbar[i].Q<Slot>();
            slot.slotName = "Slot " + i;

            // Adiciona um evento de click no slot
            hotbar[i].RegisterCallback<ClickEvent>(evt => {
                Debug.Log(slot.slotName);
            });

            // Cria uma nova imagem e adiciona no slot da hotbar
            Image icon = new Image();
            hotbar[i].Add(icon);

            // Adiciona o slot na lista de slots
            slots.Add(slot); 
        }

        Image image = hotbar[2].Q<Image>();
        image.image = testItem.icon;
    }

}
