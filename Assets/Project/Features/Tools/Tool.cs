using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LumberJack.Core;
using LumberJack.Inventory;

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
        UIDisplays.Instance.ShowKeyInfo($"Press {InputManager.Instance.InteractKeyLabel} to pick up the tool");
    }

    public void Interact()
    {
        // So some do mundo se coube no inventario
        if (InventoryManager.Instance.TryAddItem(itemReference.Item)) Destroy(gameObject);
    }
}
