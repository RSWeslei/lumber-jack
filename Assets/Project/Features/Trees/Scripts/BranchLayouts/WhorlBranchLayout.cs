using System;
using System.Collections.Generic;
using LumberJack.Core;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Galhos em anéis espaçados ao longo do pai, como em pinheiros e abetos.
    /// </summary>
    [Serializable]
    public sealed class WhorlBranchLayout : IBranchLayout
    {
        private const int MaxBranchesPerWhorl = 12;
        private const int MaxWhorls = 24;
        private const float MinWhorlSpacing = 0.1f;
        // A altura de cada anel varia no máximo esta fração do espaçamento
        private const float HeightJitterFraction = 0.3f;

        [Tooltip("Galhos em cada anel")]
        [SerializeField] private IntRange _branchesPerWhorl = new IntRange(3, 5);
        [Tooltip("Distância (m) entre os anéis")]
        [Min(MinWhorlSpacing)]
        [SerializeField] private float _whorlSpacing = 0.9f;
        [Tooltip("Trecho do pai com anéis (0 = base, 1 = ponta)")]
        [RangeLimits(0f, 1f)]
        [SerializeField] private FloatRange _span = new FloatRange(0.3f, 0.95f);
        [Tooltip("Variação da altura e da direção dos galhos (0 = anéis perfeitos)")]
        [Range(0f, 1f)]
        [SerializeField] private float _jitter = 0.25f;

        public WhorlBranchLayout()
        {
        }

        public WhorlBranchLayout(IntRange branchesPerWhorl, float whorlSpacing, FloatRange span, float jitter)
        {
            _branchesPerWhorl = branchesPerWhorl;
            _whorlSpacing = whorlSpacing;
            _span = span;
            _jitter = jitter;
        }

        public void Layout(float parentLength, ref SeededRandom random, List<BranchAttachment> results)
        {
            float spanLength = parentLength * (_span.Max - _span.Min);
            int whorls = Mathf.Clamp(Mathf.FloorToInt(spanLength / _whorlSpacing), 1, MaxWhorls);
            float heightJitter = _jitter * HeightJitterFraction * _whorlSpacing / Mathf.Max(parentLength, MinWhorlSpacing);

            for (int whorl = 0; whorl < whorls; whorl++)
            {
                float position = _span.Lerp((whorl + 0.5f) / whorls);
                int count = _branchesPerWhorl.Pick(ref random);
                if (count <= 0) continue;

                float spacing = Mathf.PI * 2f / count;
                float startAzimuth = random.NextAngle();
                for (int i = 0; i < count; i++)
                {
                    float azimuth = startAzimuth + i * spacing + random.NextSigned() * _jitter * spacing * 0.5f;
                    float height = Mathf.Clamp01(position + random.NextSigned() * heightJitter);
                    results.Add(new BranchAttachment(height, azimuth));
                }
            }
        }

        public void Validate()
        {
            _branchesPerWhorl = _branchesPerWhorl.Clamped(0, MaxBranchesPerWhorl);
            _whorlSpacing = Mathf.Max(_whorlSpacing, MinWhorlSpacing);
            _span = _span.Clamped(0f, 1f);
        }
    }
}
