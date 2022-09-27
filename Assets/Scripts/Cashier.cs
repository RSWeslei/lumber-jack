using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cashier : MonoBehaviour
{
    [SerializeField] private float totalCost = 0f;
    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Buyable")) {
            Buyable buyable = other.GetComponent<Buyable>();
            totalCost += buyable.BuyableItem.itemPrice;
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.CompareTag("Buyable")) {
            Buyable buyable = other.GetComponent<Buyable>();
            totalCost -= buyable.BuyableItem.itemPrice;
        }
    }
}
