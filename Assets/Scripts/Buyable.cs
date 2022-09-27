using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Buyable : MonoBehaviour
{
    [SerializeField] private SOBuyable buyable;

    public SOBuyable BuyableItem => buyable;

    private void Awake() {
        
    }
}
