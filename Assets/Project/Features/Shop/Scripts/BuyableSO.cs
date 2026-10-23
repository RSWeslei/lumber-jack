using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LumberJack.Inventory;

[CreateAssetMenu(fileName = "New Buyable", menuName = "Inventory/Buyable")]
public class BuyableSO : ItemDefinition
{
    public int buyPrice;
}
