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
        [SerializeField] private PlayerStats _playerStats;
        [Header("UI Elements")]
        private Label _moneyText;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
            var root = GetComponent<UIDocument>().rootVisualElement;
            _moneyText = root.Q<Label>("player-money");
        }

        public void UpdateMoneyText(float money)
        {
            _moneyText.text = money.ToString();
        }

        public void ToggleElement(GameObject element, bool toggle)
        {
            element.SetActive(toggle);
        }
    }
}

