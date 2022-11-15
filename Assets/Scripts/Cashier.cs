using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Cashier : MonoBehaviour
{
    [SerializeField] private List<Buyable> buyables;
    [SerializeField] private float totalCost = 0f;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private TextMeshPro totalCostText;

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Buyable")) {
            Buyable buyable = other.GetComponent<Buyable>();
            if (buyable == null || buyable.IsBought) {
                return;
            }
            totalCost += buyable.BuyableItem.buyPrice;
            totalCostText.text = totalCost.ToString("F2");
            buyables.Add(buyable);
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.CompareTag("Buyable")) {
            Buyable buyable = other.GetComponent<Buyable>();
            if (buyable == null || buyable.IsBought) {
                return;
            }
            totalCost -= buyable.BuyableItem.buyPrice;
            totalCostText.text = totalCost.ToString("F2");
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
            totalCostText.text = "0.00";
            Debug.Log("Bought");
        } else {
            Debug.Log("Not enough money");
        }
    } 
}
