using LumberJack.Core;
using LumberJack.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumberJack.Shared
{
    /// <summary>
    /// HUD principal (UI Toolkit): mostra o dinheiro do jogador e se atualiza quando ele muda.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIManager : MonoBehaviour
    {
        private const string MoneyLabelName = "player-money";

        [SerializeField] private PlayerStats _playerStats;

        private Label _moneyText;

        public static UIManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            _moneyText = GetComponent<UIDocument>().rootVisualElement.Q<Label>(MoneyLabelName);
        }

        private void OnEnable()
        {
            _playerStats.MoneyChanged += HandleMoneyChanged;
        }

        private void Start()
        {
            HandleMoneyChanged(_playerStats.MoneyCents);
        }

        private void OnDisable()
        {
            _playerStats.MoneyChanged -= HandleMoneyChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void HandleMoneyChanged(int cents)
        {
            _moneyText.text = Money.Format(cents);
        }
    }
}
