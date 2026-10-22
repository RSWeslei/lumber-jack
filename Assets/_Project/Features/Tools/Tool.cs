using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ItemReference))]
public class Tool : MonoBehaviour, IDisplayable, IInteractable
{
    private ItemReference itemReference;
    void Awake()
    {
        itemReference = GetComponent<ItemReference>();
    }

    public void Display()
    {
        UIDisplays.Instance.ShowKeyInfo($"Press {InputManager.Instance.interactKey} to pick up the tool");
    }

    public void Interact()
    {
        InventoryManager.Instance.AddItemToInventory(itemReference.Item);
        Destroy(gameObject);
    }
}
