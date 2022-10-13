using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Buyable : MonoBehaviour, IDisplayable
{
    [SerializeField] private SOBuyable buyable;
    private bool isBought = false;

    public SOBuyable BuyableItem => buyable;
    public bool IsBought => isBought;

    public void Buy() {
        isBought = true;
        GetComponent<MeshRenderer>().material.color = Color.green;
    }

    public void Display() {
        if (isBought) {
            UIDisplays.Instance.ShowKeyText("Press E to open the box");
        } else {
            UIDisplays.Instance.ShowKeyText("Press E to check");
            UIDisplays.Instance.DisplayBuyableInfo(buyable);
        }
    }

    public void Hide() {
        UIDisplays.Instance.HideBuyableInfo();
    }
}
