using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Managers;

public class PlayerStats : MonoBehaviour
{
    [Header("Player Stats")]
    [SerializeField] private float _life;
    [SerializeField] private float _maxLife;
    [SerializeField] private float _money=1000f;

    private void Start() 
    {
        UIManager.Instance.UpdateMoneyText(_money);
    }
    
    public void AddMoney(float amount) 
    {
        _money += amount;
        UIManager.Instance.UpdateMoneyText(_money);
    }

    public void RemoveMoney(float amount) 
    {
        _money -= amount;
        UIManager.Instance.UpdateMoneyText(_money);
    }

    public float Money => _money;
}
