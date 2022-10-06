using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cashier : MonoBehaviour
{
    [SerializeField] private List<Buyable> buyables;
    [SerializeField] private float totalCost = 0f;
    [SerializeField] private PlayerStats playerStats;

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Buyable")) {
            Buyable buyable = other.GetComponent<Buyable>();
            if (buyable == null || buyable.IsBought) {
                return;
            }
            totalCost += buyable.BuyableItem.itemPrice;
            buyables.Add(buyable);
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.CompareTag("Buyable")) {
            Buyable buyable = other.GetComponent<Buyable>();
            if (buyable == null || buyable.IsBought) {
                return;
            }
            totalCost -= buyable.BuyableItem.itemPrice;
            buyables.Remove(buyable);
        }
    }

    public void Buy () {
        if (buyables.Count == 0) {
            return;
        }
        if (playerStats.Money >= totalCost) {
            playerStats.RemoveMoney(totalCost);
            foreach (Buyable buyable in buyables) {
                buyable.Buy();
            }
            buyables = new List<Buyable>();
            totalCost = 0f;
            Debug.Log("Bought");
        } else {
            Debug.Log("Not enough money");
        }
    }
    
}
