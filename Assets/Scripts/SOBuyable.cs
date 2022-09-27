using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Buyable", menuName = "Buyable")]
public class SOBuyable : ScriptableObject
{
    public string itemName;
    public int itemPrice;
    public Texture2D itemImage;
    public GameObject itemPrefab;
}
