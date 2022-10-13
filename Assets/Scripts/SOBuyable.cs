using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "New Buyable", menuName = "Buyable")]
public class SOBuyable : ScriptableObject
{
    public string itemName;
    public int itemPrice;
    public Image itemImage;
    public GameObject itemPrefab;
    [TextArea]
    public string itemDescription;
}
