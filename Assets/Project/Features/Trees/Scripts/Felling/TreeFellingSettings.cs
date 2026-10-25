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
        private const string PieceMaterialName = "TreePiece";

        [Tooltip("Dano para cortar por m² de seção do galho/tronco")]
        [Min(0f)]
        [SerializeField] private float _healthPerSquareMeter = 15f;
        [Tooltip("Dano mínimo de um corte (galhos finos)")]
        [Min(MinHealthLimit)]
        [SerializeField] private float _minHealth = 2f;
        [Tooltip("Inclinação máxima do corte (graus)")]
        [Range(0f, MaxCutTiltLimit)]
        [SerializeField] private float _maxCutTilt = 25f;
        [Tooltip("Altura mínima (m) do toco: o tronco não é cortado mais baixo que isso")]
        [Min(0f)]
        [SerializeField] private float _minStumpHeight = 0.3f;
        [Tooltip("Cada lado do corte precisa ter pelo menos isso (m), como nas toras")]
        [Min(0f)]
        [SerializeField] private float _minCutThickness = 0.08f;

        [Header("Pedaços")]
        [Tooltip("Densidade da madeira (kg/m³)")]
        [Min(MinDensity)]
        [SerializeField] private float _density = 650f;
        [Tooltip("Velocidade inicial do tombo (graus/s), girando sobre a base")]
        [Min(0f)]
        [SerializeField] private float _tipSpeed = 30f;
        [Tooltip("Na queda, galho mais fino que isto (raio, m) quebra e some ao bater em algo; mais grosso, a árvore deitou")]
        [Min(0f)]
        [SerializeField] private float _branchBreakRadius = 0.1f;
        [Tooltip("Fração do giro da queda que sobra quando a árvore deita (0 = para na hora, sem quique)")]
        [Range(0f, 1f)]
        [SerializeField] private float _landingSpinKeep = 0.15f;
        [Tooltip("Galhos mais finos que isto (m) não colidem")]
        [Min(MinColliderRadiusLimit)]
        [SerializeField] private float _minColliderRadius = 0.05f;
        [Tooltip("Pedaços com volume menor que isto (m³) somem ao serem cortados")]
        [Min(0f)]
        [SerializeField] private float _minPieceVolume = 0.002f;
        [Tooltip("Atrito dos pedaços com o chão (alto = param logo, sem escorregar)")]
        [Range(0f, 1f)]
        [SerializeField] private float _friction = 0.9f;
        [Tooltip("Velocidade máxima de giro dos pedaços (graus/s)")]
        [Min(0f)]
        [SerializeField] private float _maxSpinSpeed = 230f;
        [Tooltip("Velocidade máxima (m/s) com que a física separa pedaços que se atravessaram (baixa = sem explosões)")]
        [Min(0.1f)]
        [SerializeField] private float _maxSeparationSpeed = 1f;

        // Criado uma vez por espécie na primeira vez que um pedaço precisa dele
        [NonSerialized] private PhysicsMaterial _pieceMaterial;

        public float MaxCutTilt => _maxCutTilt;
        public float MinStumpHeight => _minStumpHeight;
        public float MinCutThickness => _minCutThickness;
        public float MinPieceVolume => _minPieceVolume;
        public float Density => _density;
        public float TipSpeed => _tipSpeed;
        public float BranchBreakRadius => _branchBreakRadius;
        public float LandingSpinKeep => _landingSpinKeep;
        public float MinColliderRadius => _minColliderRadius;
        public float MaxSpinSpeed => _maxSpinSpeed;
        public float MaxSeparationSpeed => _maxSeparationSpeed;

        /// <summary>Material físico dos pedaços: atrito alto e nenhum quique.</summary>
        public PhysicsMaterial PieceMaterial
        {
            get
            {
                if (_pieceMaterial == null)
                {
                    _pieceMaterial = new PhysicsMaterial(PieceMaterialName)
                    {
                        dynamicFriction = _friction,
                        staticFriction = _friction,
                        bounciness = 0f,
                        frictionCombine = PhysicsMaterialCombine.Maximum,
                        bounceCombine = PhysicsMaterialCombine.Minimum
                    };
                }
                return _pieceMaterial;
            }
        }

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
            // Atrito mudou no inspector: o material é recriado com o valor novo no próximo pedaço
            _pieceMaterial = null;
        }
    }
}
