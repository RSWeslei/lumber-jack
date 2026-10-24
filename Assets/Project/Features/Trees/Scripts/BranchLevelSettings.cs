using System;
using LumberJack.Core;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Um nível de galhos: o primeiro nível nasce no tronco, o segundo nos galhos do primeiro, e assim por diante.
    /// </summary>
    [Serializable]
    public sealed class BranchLevelSettings
    {
        private const float MinLengthRatio = 0.05f;
        private const float MaxLengthRatio = 2f;
        private const float MinRadiusRatio = 0.1f;
        private const float MaxRadiusRatio = 1f;
        private const float MaxAngle = 120f;

        [Tooltip("Onde os galhos nascem no pai")]
        [SerializeReference, SubclassSelector] private IBranchLayout _layout = new SpiralBranchLayout();

        [Header("Tamanho")]
        [Tooltip("Comprimento em relação ao pai")]
        [RangeLimits(MinLengthRatio, MaxLengthRatio)]
        [SerializeField] private FloatRange _lengthRatio = new FloatRange(0.3f, 0.5f);
        [Tooltip("Comprimento conforme onde nasce no pai (0 = base, 1 = ponta)")]
        [SerializeField] private AnimationCurve _lengthOverParent = AnimationCurve.Constant(0f, 1f, 1f);
        [Tooltip("Raio na base em relação ao raio do pai onde ele nasce")]
        [RangeLimits(MinRadiusRatio, MaxRadiusRatio)]
        [SerializeField] private FloatRange _radiusRatio = new FloatRange(0.4f, 0.6f);
        [Tooltip("Multiplica o raio da base ao longo do galho (0 = base, 1 = ponta)")]
        [SerializeField] private AnimationCurve _radiusOverLength = AnimationCurve.Linear(0f, 1f, 1f, 0.2f);

        [Header("Direção")]
        [Tooltip("Ângulo (graus) entre o galho e o pai")]
        [RangeLimits(0f, MaxAngle)]
        [SerializeField] private FloatRange _angle = new FloatRange(35f, 60f);
        [Tooltip("Curva por metro: negativo pende para baixo, positivo sobe")]
        [Range(-1f, 1f)]
        [SerializeField] private float _gravity = 0.05f;
        [Tooltip("Quanto o galho entorta (graus por metro, aleatório)")]
        [Range(0f, 45f)]
        [SerializeField] private float _curvature = 8f;

        [Header("Junção com o pai")]
        [Tooltip("Quanto o galho sai alinhado com o pai antes de abrir (0 = já sai no ângulo)")]
        [Range(0f, 1f)]
        [SerializeField] private float _jointSmoothing = 0.5f;
        [Tooltip("Raio extra na base do galho (0.3 = 30% mais grosso)")]
        [Range(0f, 1.5f)]
        [SerializeField] private float _collar = 0.3f;

        public IBranchLayout Layout => _layout;
        public FloatRange LengthRatio => _lengthRatio;
        public AnimationCurve LengthOverParent => _lengthOverParent;
        public FloatRange RadiusRatio => _radiusRatio;
        public AnimationCurve RadiusOverLength => _radiusOverLength;
        public FloatRange Angle => _angle;
        public float Gravity => _gravity;
        public float Curvature => _curvature;
        public float JointSmoothing => _jointSmoothing;
        public float Collar => _collar;

        public void Validate()
        {
            _lengthRatio = _lengthRatio.Clamped(MinLengthRatio, MaxLengthRatio);
            _radiusRatio = _radiusRatio.Clamped(MinRadiusRatio, MaxRadiusRatio);
            _angle = _angle.Clamped(0f, MaxAngle);
            if (_lengthOverParent == null || _lengthOverParent.length == 0) _lengthOverParent = AnimationCurve.Constant(0f, 1f, 1f);
            if (_radiusOverLength == null || _radiusOverLength.length == 0) _radiusOverLength = AnimationCurve.Linear(0f, 1f, 1f, 0.2f);
            _layout?.Validate();
        }
    }
}
