using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Buyable : MonoBehaviour, IDisplayable
{
    [SerializeField] private SOBuyable _buyableSO;
    private bool _isBought = false;

    public SOBuyable BuyableItem => _buyableSO;
    public bool IsBought => _isBought;

    public void Buy() 
    {
        _isBought = true;
        GetComponent<MeshRenderer>().material.color = Color.green;
    }

    public void Display() 
    {
        if (_isBought) {
            UIDisplays.Instance.ShowKeyText("Press E to open the box");
        } else {
            UIDisplays.Instance.ShowKeyText("Press E to check");
            UIDisplays.Instance.DisplayBuyableInfo(_buyableSO);
        }
    }

    public void Hide() 
    {
        UIDisplays.Instance.HideBuyableInfo();
    }
}
