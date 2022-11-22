using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

public class Slot : VisualElement
{
    private Image _icon;
    public string slotName;
    private Item _item;

    public Item Item { get => _item; }

    public Slot() // Construtor
    {
        // Cria uma nova imagem e adiciona no slot da hotbar
        _icon = new Image(); 
        _icon.name = "icon"; // Nome do elemento
        _icon.AddToClassList("slotIcon"); // Adiciona uma classe CSS ao icone
        AddToClassList("slot"); // Adiciona uma classe CSS ao slot
        this.slotName = "Slot " + this.name; // Muda o nome do slot
        RegisterCallback<ClickEvent>(evt => { 
            Debug.Log(slotName);
        });
        Add(_icon);
    }

    public void AddItem(Item item) {
        _icon.image = item.icon;
        this._item = item;
    }

    public void RemoveItem() {
        _icon.image = null;
        this._item = null;
    }
    
    #region UXML
    [Preserve]
    public new class UxmlFactory : UxmlFactory<Slot, UxmlTraits> { }
    [Preserve]
    public new class UxmlTraits : VisualElement.UxmlTraits { }
    #endregion
}



