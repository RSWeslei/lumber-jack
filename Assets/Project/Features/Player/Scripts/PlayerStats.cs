using System;
using Managers;
using UnityEngine;

namespace LumberJack.Player
{
    /// <summary>
    /// Dinheiro (e futuramente vida) do jogador. Dinheiro sempre em centavos inteiros.
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        [Header("Player Stats")]
        [SerializeField] private float _life;
        [SerializeField] private float _maxLife;
        [Tooltip("Saldo inicial em centavos")]
        [SerializeField] private int _moneyCents = 100000;

        public int MoneyCents => _moneyCents;
        public float Life => _life;
        public float MaxLife => _maxLife;

        /// <summary>Disparado com o novo saldo (centavos) sempre que ele muda.</summary>
        public event Action<int> MoneyChanged;

        private void Start()
        {
            UIManager.Instance.UpdateMoneyText(_moneyCents);
        }

        public void AddMoney(int cents)
        {
            if (cents <= 0) return;

            _moneyCents += cents;
            NotifyMoneyChanged();
        }

        /// <summary>
        /// Desconta o valor se houver saldo. Retorna false (sem descontar nada) se faltar dinheiro.
        /// </summary>
        public bool TrySpend(int cents)
        {
            if (cents < 0 || cents > _moneyCents) return false;

            _moneyCents -= cents;
            NotifyMoneyChanged();
            return true;
        }

        private void NotifyMoneyChanged()
        {
            UIManager.Instance.UpdateMoneyText(_moneyCents);
            MoneyChanged?.Invoke(_moneyCents);
        }
    }
}
