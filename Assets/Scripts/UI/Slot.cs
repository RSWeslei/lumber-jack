using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

public class Slot : VisualElement
{
    private Image icon;
    public string slotName;
    private Item item;

    public Item Item { get => item; }

    public Slot() // Construtor
    {
        // Cria uma nova imagem e adiciona no slot da hotbar
        icon = new Image(); 
        icon.name = "icon"; // Nome do elemento
        icon.AddToClassList("slotIcon"); // Adiciona uma classe CSS ao icone
        AddToClassList("slot"); // Adiciona uma classe CSS ao slot
        this.slotName = "Slot " + this.name; // Muda o nome do slot
        RegisterCallback<ClickEvent>(evt => { 
            Debug.Log(slotName);
        });
        Add(icon);
    }

    public void AddItem(Item item) {
        icon.image = item.icon;
        this.item = item;
    }

    public void RemoveItem() {
        icon.image = null;
        this.item = null;
    }
    
    #region UXML
    [Preserve]
    public new class UxmlFactory : UxmlFactory<Slot, UxmlTraits> { }
    [Preserve]
    public new class UxmlTraits : VisualElement.UxmlTraits { }
    #endregion
}



