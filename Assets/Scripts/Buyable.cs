using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Buyable : MonoBehaviour
{
    [SerializeField] private SOBuyable buyable;
    private bool isBought = false;

    public SOBuyable BuyableItem => buyable;
    public bool IsBought => isBought;

    public void Buy() {
        isBought = true;
        GetComponent<MeshRenderer>().material.color = Color.green;
    }
    
}
