using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Managers 
{
    public class UIManager : MonoBehaviour
    {   
        public static UIManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private PlayerStats playerStats;
        [Header("UI Elements")]
        private Label moneyText;

        private void Awake() {
            if (Instance == null) {
                Instance = this;
            } else {
                Destroy(gameObject);
            }
            var root = GetComponent<UIDocument>().rootVisualElement;
            moneyText = root.Q<Label>("player-money");
        }

        public void UpdateMoneyText(float money) {
            moneyText.text = money.ToString();
        }

        public void ToggleElement(GameObject element, bool toggle) {
            element.SetActive(toggle);
        }
    }
}

