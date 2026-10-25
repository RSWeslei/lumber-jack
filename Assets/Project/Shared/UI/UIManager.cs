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

        private static UIManager _instance;

        private Label _moneyText;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;
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
            if (_instance == this) _instance = null;
        }

        private void HandleMoneyChanged(int cents)
        {
            _moneyText.text = Money.Format(cents);
        }
    }
}
