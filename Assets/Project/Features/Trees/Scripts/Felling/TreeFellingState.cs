using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Corte em andamento numa árvore em pé. Lógica pura: cada golpe é um comando pequeno e determinístico
    /// (dano), fácil de replicar na rede.
    /// </summary>
    public sealed class TreeFellingState
    {
        public bool IsActive { get; private set; }
        public float MaxHealth { get; private set; }
        public float Health { get; private set; }

        /// <summary>0 = começando, 1 = cortado.</summary>
        public float Progress => IsActive && MaxHealth > 0f ? 1f - Health / MaxHealth : 0f;

        /// <summary>Começa um corte novo que precisa desse dano total.</summary>
        public void Begin(float maxHealth)
        {
            IsActive = true;
            MaxHealth = maxHealth;
            Health = maxHealth;
        }

        /// <summary>Aplica um golpe. Retorna true no golpe que termina o corte.</summary>
        public bool ApplyHit(float damage)
        {
            if (!IsActive || damage <= 0f) return false;

            Health = Mathf.Max(0f, Health - damage);
            return Health <= 0f;
        }

        public void Clear()
        {
            IsActive = false;
            MaxHealth = 0f;
            Health = 0f;
        }
    }
}
