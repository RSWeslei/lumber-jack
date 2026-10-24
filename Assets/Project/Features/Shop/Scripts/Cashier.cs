using System.Collections.Generic;
using LumberJack.Core;
using LumberJack.Player;
using TMPro;
using UnityEngine;

namespace LumberJack.Shop
{
    /// <summary>
    /// Balcão do caixa: soma as caixas colocadas em cima e cobra tudo de uma vez.
    /// </summary>
    public class Cashier : MonoBehaviour
    {
        private const string BuyableTag = "Buyable";
        private const string TotalFormat = "{0:2}";

        [SerializeField] private PlayerStats _playerStats;
        [SerializeField] private TextMeshPro _totalCostText;

        private readonly List<Buyable> _pendingItems = new List<Buyable>();
        private int _totalCents;

        private void Start()
        {
            UpdateTotalText();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!TryGetUnpaidBuyable(other, out Buyable buyable)) return;

            _pendingItems.Add(buyable);
            _totalCents += buyable.Definition.PriceCents;
            UpdateTotalText();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!TryGetUnpaidBuyable(other, out Buyable buyable) || !_pendingItems.Remove(buyable)) return;

            _totalCents -= buyable.Definition.PriceCents;
            UpdateTotalText();
        }

        /// <summary>
        /// Cobra o total. Retorna false se não há nada no balcão ou falta dinheiro.
        /// </summary>
        public bool TryCheckout()
        {
            if (_pendingItems.Count == 0 || !_playerStats.TrySpend(_totalCents)) return false;

            foreach (Buyable buyable in _pendingItems)
            {
                buyable.MarkAsBought();
            }
            _pendingItems.Clear();
            _totalCents = 0;
            UpdateTotalText();
            return true;
        }

        private static bool TryGetUnpaidBuyable(Collider other, out Buyable buyable)
        {
            buyable = null;
            return other.CompareTag(BuyableTag) && other.TryGetComponent(out buyable) && !buyable.IsBought;
        }

        private void UpdateTotalText()
        {
            _totalCostText.SetText(TotalFormat, Money.ToUnits(_totalCents));
        }
    }
}
