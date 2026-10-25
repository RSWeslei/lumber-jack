using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Corte em andamento numa árvore em pé. Lógica pura: cada golpe é um comando pequeno e determinístico
    /// (dano), fácil de replicar na rede.
    /// </summary>
    public sealed class TreeFellingState
    {
        private float _maxHealth;
        private float _health;

        public bool IsActive { get; private set; }

        /// <summary>0 = começando, 1 = cortado.</summary>
        public float Progress => IsActive && _maxHealth > 0f ? 1f - _health / _maxHealth : 0f;

        /// <summary>Começa um corte novo que precisa desse dano total.</summary>
        public void Begin(float maxHealth)
        {
            IsActive = true;
            _maxHealth = maxHealth;
            _health = maxHealth;
        }

        /// <summary>Aplica um golpe. Retorna true no golpe que termina o corte.</summary>
        public bool ApplyHit(float damage)
        {
            if (!IsActive || damage <= 0f) return false;

            _health = Mathf.Max(0f, _health - damage);
            return _health <= 0f;
        }

        public void Clear()
        {
            IsActive = false;
            _maxHealth = 0f;
            _health = 0f;
        }
    }
}
