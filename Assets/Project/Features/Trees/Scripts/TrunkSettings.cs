    using System;
using LumberJack.Core;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Forma do tronco principal de uma espécie.
    /// </summary>
    [Serializable]
    public sealed class TrunkSettings
    {
        private const float MinHeight = 1f;
        private const float MaxHeight = 40f;
        private const float MinRadius = 0.05f;
        private const float MaxRadius = 1.5f;
        private const float MinFlareHeight = 0.05f;

        [Tooltip("Altura do tronco (m)")]
        [RangeLimits(MinHeight, MaxHeight)]
        [SerializeField] private FloatRange _height = new FloatRange(6f, 9f);
        [Tooltip("Raio na base (m)")]
        [RangeLimits(MinRadius, MaxRadius)]
        [SerializeField] private FloatRange _baseRadius = new FloatRange(0.2f, 0.3f);
        [Tooltip("Multiplica o raio da base ao longo do tronco (0 = base, 1 = topo)")]
        [SerializeField] private AnimationCurve _radiusOverLength = AnimationCurve.Linear(0f, 1f, 1f, 0.25f);

        [Header("Direção")]
        [Tooltip("Inclinação inicial máxima (graus)")]
        [Range(0f, 45f)]
        [SerializeField] private float _maxLean = 4f;
        [Tooltip("Quanto o tronco entorta ao subir (graus por metro, aleatório)")]
        [Range(0f, 45f)]
        [SerializeField] private float _curvature = 3f;
        [Tooltip("Quanto o tronco volta a apontar para cima a cada metro")]
        [Range(0f, 1f)]
        [SerializeField] private float _upwardPull = 0.15f;

        [Header("Raiz")]
        [Tooltip("Raio extra no pé do tronco (0.3 = 30% mais grosso)")]
        [Range(0f, 1.5f)]
        [SerializeField] private float _rootFlare = 0.3f;
        [Tooltip("Altura (m) onde o alargamento da raiz termina")]
        [Min(MinFlareHeight)]
        [SerializeField] private float _rootFlareHeight = 0.6f;

        public FloatRange Height => _height;
        public FloatRange BaseRadius => _baseRadius;
        public AnimationCurve RadiusOverLength => _radiusOverLength;
        public float MaxLean => _maxLean;
        public float Curvature => _curvature;
        public float UpwardPull => _upwardPull;
        public float RootFlare => _rootFlare;
        public float RootFlareHeight => _rootFlareHeight;

        public void Validate()
        {
            _height = _height.Clamped(MinHeight, MaxHeight);
            _baseRadius = _baseRadius.Clamped(MinRadius, MaxRadius);
            _rootFlareHeight = Mathf.Max(_rootFlareHeight, MinFlareHeight);
            if (_radiusOverLength == null || _radiusOverLength.length == 0)
            {
                _radiusOverLength = AnimationCurve.Linear(0f, 1f, 1f, 0.25f);
            }
        }
    }
}
