using System;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Como uma espécie é cortada: resistência, inclinação do corte e física dos pedaços.
    /// </summary>
    [Serializable]
    public sealed class TreeFellingSettings
    {
        private const float MinHealthLimit = 1f;
        private const float MinDensity = 1f;
        private const float MinColliderRadiusLimit = 0.01f;
        private const float MaxCutTiltLimit = 45f;

        [Tooltip("Dano para cortar por m² de seção do galho/tronco")]
        [Min(0f)]
        [SerializeField] private float _healthPerSquareMeter = 15f;
        [Tooltip("Dano mínimo de um corte (galhos finos)")]
        [Min(MinHealthLimit)]
        [SerializeField] private float _minHealth = 2f;
        [Tooltip("Inclinação máxima do corte (graus, Q/E)")]
        [Range(0f, MaxCutTiltLimit)]
        [SerializeField] private float _maxCutTilt = 25f;

        [Header("Pedaços")]
        [Tooltip("Densidade da madeira (kg/m³)")]
        [Min(MinDensity)]
        [SerializeField] private float _density = 650f;
        [Tooltip("Velocidade inicial do tombo (graus/s), girando sobre a base")]
        [Min(0f)]
        [SerializeField] private float _tipSpeed = 30f;
        [Tooltip("Galhos mais finos que isto (m) não colidem")]
        [Min(MinColliderRadiusLimit)]
        [SerializeField] private float _minColliderRadius = 0.05f;

        public float MaxCutTilt => _maxCutTilt;
        public float Density => _density;
        public float TipSpeed => _tipSpeed;
        public float MinColliderRadius => _minColliderRadius;

        /// <summary>Dano total para cortar um galho/tronco com esse raio no ponto do corte.</summary>
        public float HealthFor(float radius)
        {
            return Mathf.Max(_minHealth, Mathf.PI * radius * radius * _healthPerSquareMeter);
        }

        public void Validate()
        {
            _minHealth = Mathf.Max(_minHealth, MinHealthLimit);
            _density = Mathf.Max(_density, MinDensity);
            _minColliderRadius = Mathf.Max(_minColliderRadius, MinColliderRadiusLimit);
        }
    }
}
