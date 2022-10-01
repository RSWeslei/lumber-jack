using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Managers;

public class Player : MonoBehaviour
{
    [Header("Player Stats")]
    [SerializeField] private float life;
    [SerializeField] private float maxLife;
    [SerializeField] private float money=1000f;

    private void Start() {
        UIManager.Instance.UpdateMoneyText(money);
    }
    
    public void AddMoney(float amount) {
        money += amount;
        UIManager.Instance.UpdateMoneyText(money);
    }

    public void RemoveMoney(float amount) {
        money -= amount;
        UIManager.Instance.UpdateMoneyText(money);
    }

    public float Money => money;
}
