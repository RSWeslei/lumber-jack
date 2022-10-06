using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Managers 
{
    public class UIManager : MonoBehaviour
    {   
        public static UIManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private PlayerStats playerStats;
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private GameObject interactableUI;

        private void Awake() {
            if (Instance == null) {
                Instance = this;
            } else {
                Destroy(gameObject);
            }
        }

        public void UpdateMoneyText(float money) {
            moneyText.text = money.ToString();
        }

        public void ShowInteractableUI() { interactableUI.SetActive(true); }

        public void HideInteractableUI() { interactableUI.SetActive(false); }
        
    }
}

