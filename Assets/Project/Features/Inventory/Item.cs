using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public Texture2D icon;
    public string itemName;
    public GameObject itemPrefab;
    [TextArea]
    public string description;
}
