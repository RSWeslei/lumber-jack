using Managers;
using UnityEngine;

namespace LumberJack.Player
{
    /// <summary>
    /// Dinheiro (e futuramente vida) do jogador.
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        [Header("Player Stats")]
        [SerializeField] private float _life;
        [SerializeField] private float _maxLife;
        // Ainda em float; vai para centavos (int) na migracao da feature Shop
        [SerializeField] private float _money = 1000f;

        public float Money => _money;
        public float Life => _life;
        public float MaxLife => _maxLife;

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
    }
}
