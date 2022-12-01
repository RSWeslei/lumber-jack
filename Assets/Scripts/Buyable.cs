using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Managers;

public class Buyable : MonoBehaviour, IDisplayable, IInteractable
{
    [SerializeField] private BuyableSO _buyableSO;
    private bool isBought = false;

    public BuyableSO BuyableItem => _buyableSO;
    public bool IsBought => isBought;

    public void Buy() 
    {
        isBought = true;
        GetComponent<MeshRenderer>().material.color = Color.green;
    }

    private void OpenBox() 
    {
        if (!isBought) {
            return;
        }
        GameObject itemGO = Instantiate(_buyableSO.itemPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    public void Display() 
    {
        if (isBought) {
            UIDisplays.Instance.ShowKeyInfo($"Press {InputManager.Instance.interactKey} to open the box");
        } else {
            UIDisplays.Instance.ShowKeyInfo($"Press {InputManager.Instance.interactKey} to check");
            // UIDisplays.Instance.DisplayBuyableInfo(_buyableSO);
        }
    }

    public void Interact() 
    {
        if (isBought) 
        {
            OpenBox();
        } 
        else 
        {
            UIDisplays.Instance.DisplayBuyableInfo(_buyableSO);
        }
    }
}
