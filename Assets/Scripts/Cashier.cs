using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Cashier : MonoBehaviour
{
    [SerializeField] private List<Buyable> _buyables;
    [SerializeField] private float _totalCost = 0f;
    [SerializeField] private PlayerStats _playerStats;
    [SerializeField] private TextMeshPro _totalCostText;

    private void OnTriggerEnter(Collider other) 
    {
        if (other.CompareTag("Buyable")) 
        {
            Buyable buyable = other.GetComponent<Buyable>();
            if (buyable == null || buyable.IsBought) {
                return;
            }
            _totalCost += buyable.BuyableItem.buyPrice;
            _totalCostText.text = _totalCost.ToString("F2");
            _buyables.Add(buyable);
        }
    }

    private void OnTriggerExit(Collider other) 
    {
        if (other.CompareTag("Buyable")) 
        {
            Buyable buyable = other.GetComponent<Buyable>();
            if (buyable == null || buyable.IsBought) {
                return;
            }
            _totalCost -= buyable.BuyableItem.buyPrice;
            _totalCostText.text = _totalCost.ToString("F2");
            _buyables.Remove(buyable);
        }
    }

    public void Buy () 
    {
        if (_buyables.Count == 0) {
            return;
        }
        if (_playerStats.Money >= _totalCost) 
        {
            _playerStats.RemoveMoney(_totalCost);
            foreach (Buyable buyable in _buyables)
             {
                buyable.Buy();
            }
            _buyables = new List<Buyable>();
            _totalCost = 0f;
            _totalCostText.text = "0.00";
            Debug.Log("Bought");
        } else {
            Debug.Log("Not enough money");
        }
    } 
}
