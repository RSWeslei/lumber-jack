using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "New Buyable", menuName = "Inventory/Buyable")]
public class SOBuyable : Item
{
    public int buyPrice;
    public Image itemImage;
}
