using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    [SerializeField] private Hotbar hotbar;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddItemToInventory(Item item)
    {
        if (!hotbar.AddItem(item)){
            Debug.Log("Inventory is full");
            // TODO: Message to player
        }
    }
}
